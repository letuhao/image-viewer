# Check ImageViewer Services Status Script
Write-Host "🔍 Checking ImageViewer Services Status..." -ForegroundColor Cyan

# Check MongoDB
Write-Host ""
Write-Host "📊 MongoDB Status:" -ForegroundColor Yellow
try {
    $mongoProcess = Get-Process -Name "mongod" -ErrorAction SilentlyContinue
    if ($mongoProcess) {
        Write-Host "   ✅ MongoDB: Running (PID: $($mongoProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "   ❌ MongoDB: Not running" -ForegroundColor Red
    }
} catch {
    Write-Host "   ⚠️ Could not check MongoDB process" -ForegroundColor Yellow
}

# Test MongoDB connection
try {
    $mongoTest = Invoke-WebRequest -Uri "http://localhost:27017" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ MongoDB Port 27017: Accessible" -ForegroundColor Green
} catch {
    Write-Host "   ❌ MongoDB Port 27017: Not accessible" -ForegroundColor Red
}

# Check RabbitMQ
Write-Host ""
Write-Host "🐰 RabbitMQ Status:" -ForegroundColor Yellow
try {
    $rabbitProcess = Get-Process -Name "rabbitmq-server" -ErrorAction SilentlyContinue
    if ($rabbitProcess) {
        Write-Host "   ✅ RabbitMQ: Running (PID: $($rabbitProcess.Id))" -ForegroundColor Green
    } else {
        # Try alternative process names
        $rabbitAlt = Get-Process -Name "beam.smp" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*rabbit*" }
        if ($rabbitAlt) {
            Write-Host "   ✅ RabbitMQ: Running (PID: $($rabbitAlt.Id))" -ForegroundColor Green
        } else {
            Write-Host "   ❌ RabbitMQ: Not running" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "   ⚠️ Could not check RabbitMQ process" -ForegroundColor Yellow
}

# Test RabbitMQ connection
try {
    $rabbitTest = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ RabbitMQ Management UI: Accessible" -ForegroundColor Green
} catch {
    Write-Host "   ❌ RabbitMQ Management UI: Not accessible" -ForegroundColor Red
}

# Check running jobs
Write-Host ""
Write-Host "📋 Background Jobs:" -ForegroundColor Yellow
$jobs = Get-Job
if ($jobs.Count -gt 0) {
    $jobs | Format-Table Id, Name, State, HasMoreData -AutoSize
} else {
    Write-Host "   No background jobs running" -ForegroundColor Gray
}

# Check API server health
Write-Host ""
Write-Host "🌐 API Server Health Check:" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11000/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ API Server: Running (HTTP 200)" -ForegroundColor Green
        Write-Host "   🌐 URL: http://localhost:11000" -ForegroundColor Cyan
        Write-Host "   🔒 HTTPS URL: https://localhost:11001" -ForegroundColor Cyan
    } else {
        Write-Host "   ⚠️ API Server: Responding but status $($response.StatusCode)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ API Server: Not responding" -ForegroundColor Red
}

# Check dotnet processes
Write-Host ""
Write-Host "🔧 Dotnet Processes:" -ForegroundColor Yellow
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.CommandLine -like "*ImageViewer*" 
}
if ($dotnetProcesses.Count -gt 0) {
    $dotnetProcesses | ForEach-Object {
        $serviceType = if ($_.CommandLine -like "*ImageViewer.Api*") { "API" } 
                      elseif ($_.CommandLine -like "*ImageViewer.Worker*") { "Worker" } 
                      else { "Unknown" }
        Write-Host "   ✅ $serviceType Service: PID $($_.Id)" -ForegroundColor Green
    }
} else {
    Write-Host "   ❌ No ImageViewer dotnet processes found" -ForegroundColor Red
}

# Check ports
Write-Host ""
Write-Host "🔌 Port Status:" -ForegroundColor Yellow
$ports = @(11000, 11001)
foreach ($port in $ports) {
    $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
    if ($connection) {
        Write-Host "   ✅ Port $port: In use" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Port $port: Not in use" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "💡 To start services: .\deploy-quick.ps1" -ForegroundColor Cyan
Write-Host "💡 To stop services: .\stop-services.ps1" -ForegroundColor Cyan
