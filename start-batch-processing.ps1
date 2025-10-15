# PowerShell script to start worker with batch processing enabled
# This script optimizes the worker for maximum performance

Write-Host "🚀 Starting ImageViewer Worker with Batch Processing Optimization..." -ForegroundColor Green

# Set environment variables for batch processing
$env:UseBatchProcessing = "true"
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Set memory optimization settings
$env:DOTNET_gcServer = "1"
$env:DOTNET_gcConcurrent = "1"
$env:DOTNET_gcAllowVeryLargeObjects = "1"

Write-Host "📊 Memory Optimization Settings:" -ForegroundColor Yellow
Write-Host "  - UseBatchProcessing: $env:UseBatchProcessing" -ForegroundColor Cyan
Write-Host "  - GC Server: $env:DOTNET_gcServer" -ForegroundColor Cyan
Write-Host "  - GC Concurrent: $env:DOTNET_gcConcurrent" -ForegroundColor Cyan
Write-Host "  - GC Allow Very Large Objects: $env:DOTNET_gcAllowVeryLargeObjects" -ForegroundColor Cyan

# Copy batch processing configuration
if (Test-Path "src/ImageViewer.Worker/appsettings.BatchProcessing.json") {
    Copy-Item "src/ImageViewer.Worker/appsettings.BatchProcessing.json" "src/ImageViewer.Worker/appsettings.json" -Force
    Write-Host "✅ Applied batch processing configuration" -ForegroundColor Green
} else {
    Write-Host "⚠️ Batch processing configuration not found, using default settings" -ForegroundColor Yellow
}

# Start the worker with optimized settings
Write-Host "🎯 Starting optimized worker process..." -ForegroundColor Green

try {
    #Set-Location "src/ImageViewer.Worker"
    dotnet run --configuration Release --no-build
} catch {
    Write-Host "❌ Error starting worker: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Worker process completed" -ForegroundColor Green
