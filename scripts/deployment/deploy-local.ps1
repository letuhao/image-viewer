# PowerShell script for local deployment without Docker
param(
    [switch]$Build = $false,
    [switch]$Run = $false,
    [switch]$Stop = $false,
    [switch]$Help = $false
)

if ($Help) {
    Write-Host "ImageViewer Local Deployment Script" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\deploy-local.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Yellow
    Write-Host "  -Build              Build the application" -ForegroundColor White
    Write-Host "  -Run                Run the application locally" -ForegroundColor White
    Write-Host "  -Stop               Stop the application" -ForegroundColor White
    Write-Host "  -Help               Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy-local.ps1 -Build -Run    # Build and run locally" -ForegroundColor White
    Write-Host "  .\deploy-local.ps1 -Stop          # Stop the application" -ForegroundColor White
    exit 0
}

Write-Host "🏠 ImageViewer Local Deployment" -ForegroundColor Green

# Check if .NET 8 is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET 8 is not installed. Please install .NET 8 SDK." -ForegroundColor Red
    exit 1
}

# Check if MongoDB is running
try {
    $mongoProcess = Get-Process -Name "mongod" -ErrorAction SilentlyContinue
    if ($mongoProcess) {
        Write-Host "✅ MongoDB is running (PID: $($mongoProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "⚠️ MongoDB is not running. Starting MongoDB..." -ForegroundColor Yellow
        Start-Process -FilePath "mongod" -WindowStyle Hidden
        Start-Sleep -Seconds 5
        Write-Host "✅ MongoDB started" -ForegroundColor Green
    }
} catch {
    Write-Host "❌ MongoDB is not installed or not in PATH. Please install MongoDB." -ForegroundColor Red
    exit 1
}

# Check if RabbitMQ is running
try {
    $rabbitmqProcess = Get-Process -Name "rabbitmq-server" -ErrorAction SilentlyContinue
    if ($rabbitmqProcess) {
        Write-Host "✅ RabbitMQ is running (PID: $($rabbitmqProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "⚠️ RabbitMQ is not running. Please start RabbitMQ service." -ForegroundColor Yellow
        Write-Host "   You can start it with: net start RabbitMQ" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️ RabbitMQ is not running. Please start RabbitMQ service." -ForegroundColor Yellow
}

# Create necessary directories
Write-Host "📁 Creating necessary directories..." -ForegroundColor Yellow
$directories = @("logs", "temp", "data", "D:\ImageViewerData")
foreach ($dir in $directories) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Build the application if requested
if ($Build) {
    Write-Host "🔨 Building application..." -ForegroundColor Yellow
    dotnet build src/ImageViewer.sln -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
}

# Stop the application if requested
if ($Stop) {
    Write-Host "🛑 Stopping application..." -ForegroundColor Yellow
    
    # Stop API process
    $apiProcess = Get-Process -Name "ImageViewer.Api" -ErrorAction SilentlyContinue
    if ($apiProcess) {
        $apiProcess | Stop-Process -Force
        Write-Host "  Stopped API process" -ForegroundColor Gray
    }
    
    # Stop Worker process
    $workerProcess = Get-Process -Name "ImageViewer.Worker" -ErrorAction SilentlyContinue
    if ($workerProcess) {
        $workerProcess | Stop-Process -Force
        Write-Host "  Stopped Worker process" -ForegroundColor Gray
    }
    
    Write-Host "✅ Application stopped" -ForegroundColor Green
    exit 0
}

# Run the application if requested
if ($Run) {
    Write-Host "🚀 Starting application..." -ForegroundColor Yellow
    
    # Set environment variables
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000;https://localhost:5001"
    
    # Start API
    Write-Host "  Starting API..." -ForegroundColor Gray
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/ImageViewer.Api", "--configuration", "Release" -WindowStyle Normal
    
    # Wait a bit for API to start
    Start-Sleep -Seconds 10
    
    # Start Worker
    Write-Host "  Starting Worker..." -ForegroundColor Gray
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "src/ImageViewer.Worker", "--configuration", "Release" -WindowStyle Normal
    
    Write-Host "✅ Application started successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Service Information:" -ForegroundColor Green
    Write-Host "  API: http://localhost:5000" -ForegroundColor White
    Write-Host "  API (HTTPS): https://localhost:5001" -ForegroundColor White
    Write-Host "  MongoDB: mongodb://localhost:27017" -ForegroundColor White
    Write-Host "  RabbitMQ Management: http://localhost:15672" -ForegroundColor White
    Write-Host ""
    Write-Host "📁 Windows Drives Access:" -ForegroundColor Green
    Write-Host "  D: Drive: D:\" -ForegroundColor White
    Write-Host "  I: Drive: I:\" -ForegroundColor White
    Write-Host "  J: Drive: J:\" -ForegroundColor White
    Write-Host "  K: Drive: K:\" -ForegroundColor White
    Write-Host "  L: Drive: L:\" -ForegroundColor White
    Write-Host ""
    Write-Host "🔍 To check application status:" -ForegroundColor Yellow
    Write-Host "  Get-Process -Name 'ImageViewer.*'" -ForegroundColor White
    Write-Host ""
    Write-Host "🛑 To stop the application:" -ForegroundColor Yellow
    Write-Host "  .\deploy-local.ps1 -Stop" -ForegroundColor White
}

Write-Host ""
Write-Host "🎉 Local deployment completed!" -ForegroundColor Green
