# Start ImageViewer API Server in Background
param(
    [string]$Configuration = "Release",
    [string]$Urls = "https://localhost:11001;http://localhost:11000"
)

Write-Host "🚀 Starting ImageViewer API Server..." -ForegroundColor Cyan

# Stop existing API servers
Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*ImageViewer.Api*" } | Stop-Process -Force -ErrorAction SilentlyContinue

# Change to API directory
Set-Location "src\ImageViewer.Api"

# Start API server in background
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", $Configuration, "--urls", $Urls -WindowStyle Hidden -PassThru

# Wait a moment for startup
Start-Sleep -Seconds 3

# Health check
$maxRetries = 10
$retryCount = 0
$isHealthy = $false

Write-Host "🔍 Checking API server health..." -ForegroundColor Yellow

while ($retryCount -lt $maxRetries -and -not $isHealthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:11000/health" -TimeoutSec 3 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $isHealthy = $true
        }
    } catch {
        $retryCount++
        Start-Sleep -Seconds 1
    }
}

if ($isHealthy) {
    Write-Host "✅ API server started successfully!" -ForegroundColor Green
    Write-Host "🌐 API Server: https://localhost:11001" -ForegroundColor Yellow
    Write-Host "🌐 HTTP Server: http://localhost:11000" -ForegroundColor Yellow
    Write-Host "🆔 Process ID: $($apiProcess.Id)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "💡 To stop the server: Get-Process -Id $($apiProcess.Id) | Stop-Process" -ForegroundColor Cyan
    Write-Host "💡 Or use: taskkill /F /PID $($apiProcess.Id)" -ForegroundColor Cyan
} else {
    Write-Host "❌ API server health check failed!" -ForegroundColor Red
    Write-Host "🆔 Process ID: $($apiProcess.Id) - Check if server is running manually" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ Script completed. API server is running in background." -ForegroundColor Green
