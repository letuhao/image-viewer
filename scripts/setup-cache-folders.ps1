# Setup Cache Folders and Test API
# This script sets up cache folders and tests the cache API endpoints

param(
    [string[]]$CachePaths = @("I:\Image_Cache", "J:\Image_Cache", "K:\Image_Cache", "L:\Image_Cache"),
    [string]$ApiBaseUrl = "http://localhost:11000/api"
)

Write-Host "📁 Setting up cache folders..." -ForegroundColor Green

# Create cache folders
foreach ($path in $CachePaths) {
    if (-not (Test-Path $path)) {
        try {
            New-Item -ItemType Directory -Path $path -Force | Out-Null
            Write-Host "✅ Created cache folder: $path" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Failed to create cache folder: $path - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        Write-Host "ℹ️ Cache folder already exists: $path" -ForegroundColor Yellow
    }
}

# Test cache API endpoints
Write-Host "`n🔍 Testing cache API endpoints..." -ForegroundColor Green

try {
    # Test health endpoint first
    Write-Host "🏥 Testing health endpoint..." -ForegroundColor Yellow
    $healthResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/../health" -Method GET -TimeoutSec 10
    Write-Host "✅ Health check passed" -ForegroundColor Green
    
    # Test cache folders endpoint
    Write-Host "📂 Testing cache folders endpoint..." -ForegroundColor Yellow
    $cacheFoldersResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/cache/folders" -Method GET -TimeoutSec 10
    Write-Host "✅ Cache folders API working" -ForegroundColor Green
    Write-Host "📊 Found $($cacheFoldersResponse.Count) cache folders" -ForegroundColor Cyan
    
    # Display cache folder details
    if ($cacheFoldersResponse -and $cacheFoldersResponse.Count -gt 0) {
        Write-Host "`n📋 Cache Folder Details:" -ForegroundColor Cyan
        foreach ($folder in $cacheFoldersResponse) {
            Write-Host "   📁 $($folder.Name): $($folder.Path)" -ForegroundColor Gray
            Write-Host "      💾 Max Size: $($folder.MaxSizeBytes) bytes" -ForegroundColor Gray
            Write-Host "      ⭐ Priority: $($folder.Priority)" -ForegroundColor Gray
        }
    }
    
    # Test cache statistics
    Write-Host "`n📊 Testing cache statistics..." -ForegroundColor Yellow
    $cacheStatsResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/cache/statistics" -Method GET -TimeoutSec 10
    Write-Host "✅ Cache statistics API working" -ForegroundColor Green
    
    if ($cacheStatsResponse) {
        Write-Host "📈 Cache Statistics:" -ForegroundColor Cyan
        Write-Host "   💾 Total Size: $($cacheStatsResponse.Summary.TotalSizeBytes) bytes" -ForegroundColor Gray
        Write-Host "   📁 Total Folders: $($cacheStatsResponse.Summary.TotalFolders)" -ForegroundColor Gray
        Write-Host "   🖼️ Total Images: $($cacheStatsResponse.Summary.TotalImages)" -ForegroundColor Gray
    }
    
    return @{
        Success = $true
        CacheFolders = $cacheFoldersResponse
        CacheStatistics = $cacheStatsResponse
    }
}
catch {
    Write-Host "❌ Cache API test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔍 Response: $($_.Exception.Response)" -ForegroundColor Red
    
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
}
