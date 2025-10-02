# Quick Deploy Script - API + Worker Services
Write-Host "🚀 Quick Deploy ImageViewer API + Worker Services..." -ForegroundColor Cyan

# Check dependencies first
Write-Host "🔍 Checking dependencies..." -ForegroundColor Yellow

# Check MongoDB
Write-Host "📊 Checking MongoDB..." -ForegroundColor Yellow
try {
    $mongoProcess = Get-Process -Name "mongod" -ErrorAction SilentlyContinue
    if ($mongoProcess) {
        Write-Host "   ✅ MongoDB: Running (PID: $($mongoProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "   ❌ MongoDB: Not running" -ForegroundColor Red
        Write-Host "   💡 Start MongoDB: mongod" -ForegroundColor Cyan
        Write-Host "   ⚠️ Continuing anyway - MongoDB may be running as a service" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️ Could not check MongoDB process" -ForegroundColor Yellow
}

# Check RabbitMQ
Write-Host "🐰 Checking RabbitMQ..." -ForegroundColor Yellow
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
            Write-Host "   💡 Start RabbitMQ: rabbitmq-server" -ForegroundColor Cyan
            Write-Host "   ⚠️ Continuing anyway - RabbitMQ may be running as a service" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "   ⚠️ Could not check RabbitMQ process" -ForegroundColor Yellow
}

# Test MongoDB connection
Write-Host "🔌 Testing MongoDB connection..." -ForegroundColor Yellow
try {
    $mongoTest = Invoke-WebRequest -Uri "http://localhost:27017" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ MongoDB: Port 27017 accessible" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️ MongoDB: Port 27017 not accessible (may still work)" -ForegroundColor Yellow
}

# Test RabbitMQ connection
Write-Host "🔌 Testing RabbitMQ connection..." -ForegroundColor Yellow
try {
    $rabbitTest = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ RabbitMQ: Management UI accessible" -ForegroundColor Green
} catch {
    Write-Host "   ⚠️ RabbitMQ: Management UI not accessible (may still work)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🛑 Stopping existing services..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.CommandLine -like "*ImageViewer.Api*" -or $_.CommandLine -like "*ImageViewer.Worker*" 
} | Stop-Process -Force -ErrorAction SilentlyContinue

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
Set-Location "src"
dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Start API server in background
Write-Host "🚀 Starting API server in background..." -ForegroundColor Yellow
$apiJob = Start-Job -ScriptBlock {
    Set-Location "D:\Works\source\image-viewer\src\ImageViewer.Api"
    dotnet run --configuration Release --urls "https://localhost:11001;http://localhost:11000"
}

# Start Worker service in background
Write-Host "⚙️ Starting Worker service in background..." -ForegroundColor Yellow
$workerJob = Start-Job -ScriptBlock {
    Set-Location "D:\Works\source\image-viewer\src\ImageViewer.Worker"
    dotnet run --configuration Release
}

# Wait a moment for services to start
Write-Host "⏳ Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Check if API server is running
Write-Host "🔍 Checking API server..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11000/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API server is running!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ API server may not be ready yet" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ API server health check failed, but it may still be starting..." -ForegroundColor Yellow
}

# Check worker job status
Write-Host "🔍 Checking Worker service..." -ForegroundColor Yellow
$workerState = Get-Job -Id $workerJob.Id | Select-Object -ExpandProperty State
if ($workerState -eq "Running") {
    Write-Host "✅ Worker service is running!" -ForegroundColor Green
} else {
    Write-Host "⚠️ Worker service state: $workerState" -ForegroundColor Yellow
}

# Display service information
Write-Host ""
Write-Host "🎉 Services Status:" -ForegroundColor Green
Write-Host "🌐 API Server: https://localhost:11001" -ForegroundColor Yellow
Write-Host "🌐 HTTP Server: http://localhost:11000" -ForegroundColor Yellow
Write-Host "⚙️ Worker Service: Processing background tasks" -ForegroundColor Yellow
Write-Host ""
Write-Host "📊 Dependencies:" -ForegroundColor Cyan
Write-Host "   📊 MongoDB: Port 27017" -ForegroundColor Cyan
Write-Host "   🐰 RabbitMQ: Port 5672 (Management: http://localhost:15672)" -ForegroundColor Cyan
Write-Host ""
Write-Host "🆔 Job IDs:" -ForegroundColor Cyan
Write-Host "   API Job ID: $($apiJob.Id)" -ForegroundColor Cyan
Write-Host "   Worker Job ID: $($workerJob.Id)" -ForegroundColor Cyan
Write-Host ""
Write-Host "💡 Management Commands:" -ForegroundColor Cyan
Write-Host "   Stop services: .\stop-services.ps1" -ForegroundColor Cyan
Write-Host "   Check status: .\check-services.ps1" -ForegroundColor Cyan
Write-Host "   Stop jobs: Stop-Job -Id $($apiJob.Id), $($workerJob.Id)" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Deploy completed! Both API and Worker services are running in background jobs." -ForegroundColor Green
Write-Host "💡 Script will now exit. Services continue running." -ForegroundColor Cyan
