# Stop ImageViewer API Server
Write-Host "🛑 Stopping ImageViewer API Server..." -ForegroundColor Cyan

# Find and stop dotnet processes running ImageViewer.Api
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.CommandLine -like "*ImageViewer.Api*" -or 
    $_.MainWindowTitle -like "*ImageViewer*" 
}

if ($processes) {
    foreach ($process in $processes) {
        Write-Host "🔄 Stopping process ID: $($process.Id)" -ForegroundColor Yellow
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "✅ API server stopped successfully!" -ForegroundColor Green
} else {
    Write-Host "ℹ️ No ImageViewer API server processes found" -ForegroundColor Yellow
}

# Also try to stop by port
Write-Host "🔍 Checking for processes using ports 11000/11001..." -ForegroundColor Yellow
try {
    $netstat = netstat -ano | Select-String ":11000|:11001"
    if ($netstat) {
        Write-Host "Found processes using API ports:" -ForegroundColor Yellow
        $netstat | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }
} catch {
    # Ignore errors
}

Write-Host "✅ Stop script completed" -ForegroundColor Green
