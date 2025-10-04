# PowerShell script for Docker deployment
param(
    [string]$Environment = "development",
    [switch]$Build = $false,
    [switch]$Pull = $false,
    [switch]$Force = $false,
    [switch]$Help = $false
)

if ($Help) {
    Write-Host "ImageViewer Docker Deployment Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\deploy-docker.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Environment <env>  Deployment environment (development|production) [default: development]" -ForegroundColor White
    Write-Host "  -Build              Build images before deployment" -ForegroundColor White
    Write-Host "  -Pull               Pull latest images before deployment" -ForegroundColor White
    Write-Host "  -Force              Force recreate containers" -ForegroundColor White
    Write-Host "  -Help               Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy-docker.ps1                                    # Deploy development environment" -ForegroundColor White
    Write-Host "  .\deploy-docker.ps1 -Environment production -Build    # Build and deploy production" -ForegroundColor White
    Write-Host "  .\deploy-docker.ps1 -Force                            # Force recreate all containers" -ForegroundColor White
    exit 0
}

Write-Host "🚀 ImageViewer Docker Deployment" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop." -ForegroundColor Red
    exit 1
}

# Check if Docker Compose is available
try {
    docker-compose version | Out-Null
    Write-Host "✅ Docker Compose is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Compose is not available. Please install Docker Compose." -ForegroundColor Red
    exit 1
}

# Create necessary directories
Write-Host "📁 Creating necessary directories..." -ForegroundColor Yellow
$directories = @("logs", "temp", "data", "nginx/ssl")
foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Set environment variables
$env:COMPOSE_PROJECT_NAME = "imageviewer"
$env:COMPOSE_FILE = "docker-compose.yml"

if ($Environment -eq "development") {
    $env:COMPOSE_FILE += ":docker-compose.override.yml"
    Write-Host "🔧 Using development configuration" -ForegroundColor Yellow
} else {
    Write-Host "🔧 Using production configuration" -ForegroundColor Yellow
}

# Pull latest images if requested
if ($Pull) {
    Write-Host "📥 Pulling latest images..." -ForegroundColor Yellow
    docker-compose pull
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to pull images" -ForegroundColor Red
        exit 1
    }
}

# Build images if requested
if ($Build) {
    Write-Host "🔨 Building images..." -ForegroundColor Yellow
    docker-compose build --no-cache
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to build images" -ForegroundColor Red
        exit 1
    }
}

# Stop existing containers
Write-Host "🛑 Stopping existing containers..." -ForegroundColor Yellow
docker-compose down

# Start services
Write-Host "🚀 Starting services..." -ForegroundColor Yellow
if ($Force) {
    docker-compose up -d --force-recreate
} else {
    docker-compose up -d
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to start services" -ForegroundColor Red
    exit 1
}

# Wait for services to be healthy
Write-Host "⏳ Waiting for services to be healthy..." -ForegroundColor Yellow
$maxWait = 300 # 5 minutes
$waitTime = 0
$interval = 10

do {
    Start-Sleep -Seconds $interval
    $waitTime += $interval
    
    $services = docker-compose ps --services --filter "status=running"
    $healthyServices = 0
    $totalServices = $services.Count
    
    foreach ($service in $services) {
        $health = docker-compose ps --format "table {{.Service}}\t{{.State}}" | Select-String $service
        if ($health -match "healthy|Up") {
            $healthyServices++
        }
    }
    
    Write-Host "  Health check: $healthyServices/$totalServices services healthy" -ForegroundColor Gray
    
    if ($healthyServices -eq $totalServices) {
        break
    }
    
    if ($waitTime -ge $maxWait) {
        Write-Host "❌ Services did not become healthy within $maxWait seconds" -ForegroundColor Red
        Write-Host "📋 Service status:" -ForegroundColor Yellow
        docker-compose ps
        exit 1
    }
} while ($true)

Write-Host "✅ All services are healthy!" -ForegroundColor Green

# Display service information
Write-Host ""
Write-Host "📋 Service Information:" -ForegroundColor Green
Write-Host "  API: http://localhost:5000" -ForegroundColor White
Write-Host "  API (HTTPS): https://localhost:5001" -ForegroundColor White
Write-Host "  MongoDB: mongodb://localhost:27017" -ForegroundColor White
Write-Host "  RabbitMQ Management: http://localhost:15672" -ForegroundColor White
Write-Host "  Redis: redis://localhost:6379" -ForegroundColor White

if ($Environment -eq "production") {
    Write-Host "  Nginx: http://localhost:80" -ForegroundColor White
    Write-Host "  Nginx (HTTPS): https://localhost:443" -ForegroundColor White
}

Write-Host ""
Write-Host "🔍 Checking service health..." -ForegroundColor Yellow

# Check API health
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ API is healthy" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️ API returned status: $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ API health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Check MongoDB
try {
    $mongoResponse = docker exec imageviewer-mongodb mongosh --eval "db.adminCommand('ping')" --quiet
    if ($mongoResponse -match "ok") {
        Write-Host "  ✅ MongoDB is healthy" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️ MongoDB health check failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ MongoDB health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Check RabbitMQ
try {
    $rabbitResponse = docker exec imageviewer-rabbitmq rabbitmq-diagnostics ping
    if ($rabbitResponse -match "pong") {
        Write-Host "  ✅ RabbitMQ is healthy" -ForegroundColor Green
    } else {
        Write-Host "  ⚠️ RabbitMQ health check failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ❌ RabbitMQ health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Useful commands:" -ForegroundColor Yellow
Write-Host "  View logs: docker-compose logs -f" -ForegroundColor White
Write-Host "  Stop services: docker-compose down" -ForegroundColor White
Write-Host "  Restart services: docker-compose restart" -ForegroundColor White
Write-Host "  View status: docker-compose ps" -ForegroundColor White
