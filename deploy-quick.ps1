# Quick Deploy Script - Không bị hang
Write-Host "🚀 Quick Deploy ImageViewer API..." -ForegroundColor Cyan

# Stop existing processes
Write-Host "🛑 Stopping existing API servers..." -ForegroundColor Yellow
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*ImageViewer.Api*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Build
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
Set-Location "src\ImageViewer.Api"
dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Start API server in background
Write-Host "🚀 Starting API server in background..." -ForegroundColor Yellow
$job = Start-Job -ScriptBlock {
    Set-Location "D:\Works\source\image-viewer\src\ImageViewer.Api"
    dotnet run --configuration Release --urls "https://localhost:11001;http://localhost:11000"
}

# Wait a moment
Start-Sleep -Seconds 5

# Check if server is running
Write-Host "🔍 Checking API server..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:11000/health" -TimeoutSec 5 -ErrorAction SilentlyContinue
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ API server is running!" -ForegroundColor Green
        Write-Host "🌐 API Server: https://localhost:11001" -ForegroundColor Yellow
        Write-Host "🌐 HTTP Server: http://localhost:11000" -ForegroundColor Yellow
        Write-Host "🆔 Job ID: $($job.Id)" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "💡 To stop: Stop-Job -Id $($job.Id); Remove-Job -Id $($job.Id)" -ForegroundColor Cyan
    } else {
        Write-Host "⚠️ API server may not be ready yet" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ API server health check failed, but it may still be starting..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ Deploy completed! API server is running in background job." -ForegroundColor Green
Write-Host "💡 Script will now exit. API server continues running." -ForegroundColor Cyan
