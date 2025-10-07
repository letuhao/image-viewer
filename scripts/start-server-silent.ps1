# Start ImageViewer Server in Silent Mode
# This script starts the server and monitors logs for errors

param(
    [string]$LogPath = "logs",
    [int]$MaxWaitTime = 30
)

Write-Host "🚀 Starting ImageViewer Server in Silent Mode..." -ForegroundColor Green

# Kill any existing processes
Write-Host "🔄 Stopping any existing ImageViewer processes..." -ForegroundColor Yellow
Get-Process -Name "ImageViewer.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
Get-Process -Name "ImageViewer.Worker" -ErrorAction SilentlyContinue | Stop-Process -Force

# Start API server in background
Write-Host "🌐 Starting API server..." -ForegroundColor Yellow
$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet run --project src/ImageViewer.Api --no-build --verbosity quiet
}

# Start Worker in background
Write-Host "⚙️ Starting background worker..." -ForegroundColor Yellow
$workerJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    dotnet run --project src/ImageViewer.Worker --no-build --verbosity quiet
}

# Wait for server to start
Write-Host "⏳ Waiting for server to start (max $MaxWaitTime seconds)..." -ForegroundColor Yellow
$startTime = Get-Date
$serverReady = $false

while (-not $serverReady -and ((Get-Date) - $startTime).TotalSeconds -lt $MaxWaitTime) {
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:11000/health" -Method GET -TimeoutSec 5 -ErrorAction Stop
        if ($response) {
            $serverReady = $true
            Write-Host "✅ Server is ready!" -ForegroundColor Green
        }
    }
    catch {
        Start-Sleep -Seconds 2
        Write-Host "." -NoNewline -ForegroundColor Gray
    }
}

if (-not $serverReady) {
    Write-Host "❌ Server failed to start within $MaxWaitTime seconds" -ForegroundColor Red
    Write-Host "📋 Checking logs for errors..." -ForegroundColor Yellow
    
    # Check for log files
    if (Test-Path $LogPath) {
        $latestLog = Get-ChildItem -Path $LogPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestLog) {
            Write-Host "📄 Latest log: $($latestLog.Name)" -ForegroundColor Cyan
            $logContent = Get-Content $latestLog.FullName -Tail 20
            Write-Host "🔍 Last 20 lines:" -ForegroundColor Cyan
            $logContent | ForEach-Object { Write-Host "   $_" -ForegroundColor Gray }
        }
    }
    
    # Clean up jobs
    $apiJob | Stop-Job
    $workerJob | Stop-Job
    $apiJob | Remove-Job
    $workerJob | Remove-Job
    
    exit 1
}

# Monitor logs for errors
Write-Host "📊 Monitoring server logs for errors..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Check for error logs
if (Test-Path $LogPath) {
    $errorLogs = Get-ChildItem -Path $LogPath -Filter "*error*" -ErrorAction SilentlyContinue
    if ($errorLogs) {
        Write-Host "⚠️ Found error logs:" -ForegroundColor Yellow
        $errorLogs | ForEach-Object { Write-Host "   $($_.Name)" -ForegroundColor Red }
    }
    
    $latestLog = Get-ChildItem -Path $LogPath -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        $logContent = Get-Content $latestLog.FullName -Tail 10
        $hasErrors = $logContent | Where-Object { $_ -match "ERROR|FATAL|Exception" }
        if ($hasErrors) {
            Write-Host "❌ Found errors in latest log:" -ForegroundColor Red
            $hasErrors | ForEach-Object { Write-Host "   $_" -ForegroundColor Red }
        } else {
            Write-Host "✅ No errors found in latest log" -ForegroundColor Green
        }
    }
}

Write-Host "🎯 Server is running successfully!" -ForegroundColor Green
Write-Host "📡 API Endpoint: http://localhost:11000" -ForegroundColor Cyan
Write-Host "📋 Health Check: http://localhost:11000/health" -ForegroundColor Cyan
Write-Host "📚 Swagger UI: http://localhost:11000" -ForegroundColor Cyan

# Return job objects for cleanup
return @{
    ApiJob = $apiJob
    WorkerJob = $workerJob
    ServerReady = $serverReady
}
