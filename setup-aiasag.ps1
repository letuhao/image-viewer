# ImageViewer - Setup AiASAG Collections
# This script sets up collections from L:\EMedia\AI_Generated\AiASAG folder

Write-Host "🚀 Starting AiASAG Collections Setup" -ForegroundColor Green

# Check if API is running
Write-Host "🔍 Checking if API is running..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "https://localhost:11001/api/health" -Method GET -SkipCertificateCheck
    Write-Host "✅ API is running and healthy" -ForegroundColor Green
} catch {
    Write-Host "❌ API is not running. Please start the API first using: dotnet run --project src/ImageViewer.Api" -ForegroundColor Red
    exit 1
}

# Check if target path exists
$targetPath = "L:\EMedia\AI_Generated\AiASAG"
if (-not (Test-Path $targetPath)) {
    Write-Host "❌ Target path does not exist: $targetPath" -ForegroundColor Red
    exit 1
}

Write-Host "📁 Target path exists: $targetPath" -ForegroundColor Green

# Count files and folders
$files = Get-ChildItem -Path $targetPath -Recurse -File
$folders = Get-ChildItem -Path $targetPath -Recurse -Directory
Write-Host "📊 Found $($files.Count) files and $($folders.Count) folders" -ForegroundColor Cyan

# Run the integration test
Write-Host "🧪 Running AiASAG collections setup test..." -ForegroundColor Yellow
try {
    dotnet test src/tests/ImageViewer.IntegrationTests/Setup/SetupCollectionsFromAiASAG.cs --logger "console;verbosity=detailed"
    Write-Host "✅ Setup completed successfully!" -ForegroundColor Green
} catch {
    Write-Host "❌ Setup failed. Check the logs above for details." -ForegroundColor Red
    exit 1
}

# Verify the setup
Write-Host "🔍 Verifying collections were created..." -ForegroundColor Yellow
try {
    $collectionsResponse = Invoke-RestMethod -Uri "https://localhost:11001/api/collections" -Method GET -SkipCertificateCheck
    $collectionCount = $collectionsResponse.data.Count
    Write-Host "✅ Found $collectionCount collections" -ForegroundColor Green
    
    $statsResponse = Invoke-RestMethod -Uri "https://localhost:11001/api/statistics/overall" -Method GET -SkipCertificateCheck
    Write-Host "📊 Statistics:" -ForegroundColor Cyan
    Write-Host "  - Total Collections: $($statsResponse.totalCollections)" -ForegroundColor White
    Write-Host "  - Total Images: $($statsResponse.totalImages)" -ForegroundColor White
    Write-Host "  - Total Size: $([math]::Round($statsResponse.totalSize / 1MB, 2)) MB" -ForegroundColor White
    
} catch {
    Write-Host "⚠️ Could not verify collections. API might not be running." -ForegroundColor Yellow
}

Write-Host "🎉 AiASAG Collections Setup Complete!" -ForegroundColor Green
Write-Host "You can now access the collections via the API or web interface." -ForegroundColor Cyan
