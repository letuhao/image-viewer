# Check Dependencies Script - MongoDB and RabbitMQ
Write-Host "🔍 Checking ImageViewer Dependencies..." -ForegroundColor Cyan

# MongoDB Checks
Write-Host ""
Write-Host "📊 MongoDB Status:" -ForegroundColor Yellow

# Check MongoDB process
try {
    $mongoProcess = Get-Process -Name "mongod" -ErrorAction SilentlyContinue
    if ($mongoProcess) {
        Write-Host "   ✅ MongoDB Process: Running (PID: $($mongoProcess.Id))" -ForegroundColor Green
    } else {
        Write-Host "   ❌ MongoDB Process: Not running" -ForegroundColor Red
        Write-Host "   💡 Start MongoDB: mongod" -ForegroundColor Cyan
    }
} catch {
    Write-Host "   ⚠️ Could not check MongoDB process" -ForegroundColor Yellow
}

# Test MongoDB port
try {
    $mongoTest = Invoke-WebRequest -Uri "http://localhost:27017" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ MongoDB Port 27017: Accessible" -ForegroundColor Green
} catch {
    Write-Host "   ❌ MongoDB Port 27017: Not accessible" -ForegroundColor Red
    Write-Host "   💡 Check if MongoDB is running on port 27017" -ForegroundColor Cyan
}

# RabbitMQ Checks
Write-Host ""
Write-Host "🐰 RabbitMQ Status:" -ForegroundColor Yellow

# Check RabbitMQ process
try {
    $rabbitProcess = Get-Process -Name "rabbitmq-server" -ErrorAction SilentlyContinue
    if ($rabbitProcess) {
        Write-Host "   ✅ RabbitMQ Process: Running (PID: $($rabbitProcess.Id))" -ForegroundColor Green
    } else {
        # Try alternative process names
        $rabbitAlt = Get-Process -Name "beam.smp" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*rabbit*" }
        if ($rabbitAlt) {
            Write-Host "   ✅ RabbitMQ Process: Running (PID: $($rabbitAlt.Id))" -ForegroundColor Green
        } else {
            Write-Host "   ❌ RabbitMQ Process: Not running" -ForegroundColor Red
            Write-Host "   💡 Start RabbitMQ: rabbitmq-server" -ForegroundColor Cyan
        }
    }
} catch {
    Write-Host "   ⚠️ Could not check RabbitMQ process" -ForegroundColor Yellow
}

# Test RabbitMQ management UI
try {
    $rabbitTest = Invoke-WebRequest -Uri "http://localhost:15672" -TimeoutSec 3 -ErrorAction SilentlyContinue
    Write-Host "   ✅ RabbitMQ Management UI: Accessible" -ForegroundColor Green
    Write-Host "   🌐 Management URL: http://localhost:15672" -ForegroundColor Cyan
} catch {
    Write-Host "   ❌ RabbitMQ Management UI: Not accessible" -ForegroundColor Red
    Write-Host "   💡 Check if RabbitMQ is running on port 15672" -ForegroundColor Cyan
}

# Test RabbitMQ AMQP port
try {
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $tcpClient.Connect("localhost", 5672)
    $tcpClient.Close()
    Write-Host "   ✅ RabbitMQ AMQP Port 5672: Accessible" -ForegroundColor Green
} catch {
    Write-Host "   ❌ RabbitMQ AMQP Port 5672: Not accessible" -ForegroundColor Red
    Write-Host "   💡 Check if RabbitMQ is running on port 5672" -ForegroundColor Cyan
}

# Summary
Write-Host ""
Write-Host "📋 Dependency Summary:" -ForegroundColor Green

$mongoOk = $false
$rabbitOk = $false

# Check MongoDB status
try {
    $mongoProcess = Get-Process -Name "mongod" -ErrorAction SilentlyContinue
    if ($mongoProcess) { $mongoOk = $true }
} catch { }

# Check RabbitMQ status
try {
    $rabbitProcess = Get-Process -Name "rabbitmq-server" -ErrorAction SilentlyContinue
    if ($rabbitProcess) { $rabbitOk = $true }
    if (-not $rabbitOk) {
        $rabbitAlt = Get-Process -Name "beam.smp" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*rabbit*" }
        if ($rabbitAlt) { $rabbitOk = $true }
    }
} catch { }

if ($mongoOk -and $rabbitOk) {
    Write-Host "   ✅ All dependencies are running!" -ForegroundColor Green
    Write-Host "   🚀 Ready to start ImageViewer services" -ForegroundColor Green
} else {
    Write-Host "   ❌ Some dependencies are missing" -ForegroundColor Red
    Write-Host "   💡 Start missing services before running ImageViewer" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "💡 Quick Start Commands:" -ForegroundColor Cyan
Write-Host "   Start MongoDB: mongod" -ForegroundColor Cyan
Write-Host "   Start RabbitMQ: rabbitmq-server" -ForegroundColor Cyan
Write-Host "   Deploy ImageViewer: .\deploy-quick.ps1" -ForegroundColor Cyan
