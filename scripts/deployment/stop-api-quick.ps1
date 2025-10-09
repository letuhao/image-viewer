# Stop ImageViewer API Server
Write-Host "🛑 Stopping ImageViewer API Server..." -ForegroundColor Cyan

# Stop all background jobs
$jobs = Get-Job
if ($jobs) {
    foreach ($job in $jobs) {
        Write-Host "🔄 Stopping job ID: $($job.Id)" -ForegroundColor Yellow
        Stop-Job -Id $job.Id -ErrorAction SilentlyContinue
        Remove-Job -Id $job.Id -ErrorAction SilentlyContinue
    }
    Write-Host "✅ Background jobs stopped!" -ForegroundColor Green
} else {
    Write-Host "ℹ️ No background jobs found" -ForegroundColor Yellow
}

# Stop dotnet processes
$processes = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.CommandLine -like "*ImageViewer.Api*" 
}

if ($processes) {
    foreach ($process in $processes) {
        Write-Host "🔄 Stopping process ID: $($process.Id)" -ForegroundColor Yellow
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
    Write-Host "✅ API server processes stopped!" -ForegroundColor Green
} else {
    Write-Host "ℹ️ No ImageViewer API processes found" -ForegroundColor Yellow
}

Write-Host "✅ Stop script completed" -ForegroundColor Green
