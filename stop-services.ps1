# Stop ImageViewer Services Script
Write-Host "🛑 Stopping ImageViewer Services..." -ForegroundColor Cyan

# Stop all running jobs
Write-Host "🛑 Stopping background jobs..." -ForegroundColor Yellow
Get-Job | Where-Object { $_.State -eq "Running" } | Stop-Job
Get-Job | Remove-Job

# Stop any remaining dotnet processes
Write-Host "🛑 Stopping dotnet processes..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { 
    $_.CommandLine -like "*ImageViewer.Api*" -or $_.CommandLine -like "*ImageViewer.Worker*" 
} | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "✅ All ImageViewer services stopped!" -ForegroundColor Green
