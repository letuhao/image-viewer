# Check Cache Generation in All Cache Folders
# This script verifies that cache files were generated for the collections

param(
    [string[]]$CachePaths = @("I:\Image_Cache", "J:\Image_Cache", "K:\Image_Cache", "L:\Image_Cache"),
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [int]$MaxWaitTime = 300
)

Write-Host "🔍 Checking cache generation in all cache folders..." -ForegroundColor Green

# Check each cache folder
$cacheResults = @{}
$totalCacheFiles = 0
$totalCacheSize = 0

foreach ($cachePath in $CachePaths) {
    Write-Host "`n📁 Checking cache folder: $cachePath" -ForegroundColor Yellow
    
    if (Test-Path $cachePath) {
        $cacheFiles = Get-ChildItem -Path $cachePath -Recurse -File | Where-Object { $_.Extension -match '\.(jpg|jpeg|png|webp|gif)$' }
        $cacheSize = ($cacheFiles | Measure-Object -Property Length -Sum).Sum
        
        $cacheResults[$cachePath] = @{
            Exists = $true
            FileCount = $cacheFiles.Count
            TotalSize = $cacheSize
            Files = $cacheFiles
        }
        
        $totalCacheFiles += $cacheFiles.Count
        $totalCacheSize += $cacheSize
        
        Write-Host "   📊 Files: $($cacheFiles.Count)" -ForegroundColor Cyan
        Write-Host "   💾 Size: $([math]::Round($cacheSize / 1MB, 2)) MB" -ForegroundColor Cyan
        
        if ($cacheFiles.Count -gt 0) {
            Write-Host "   📄 Sample files:" -ForegroundColor Gray
            $cacheFiles | Select-Object -First 5 | ForEach-Object { 
                Write-Host "      • $($_.Name) ($([math]::Round($_.Length / 1KB, 1)) KB)" -ForegroundColor Gray 
            }
            if ($cacheFiles.Count -gt 5) {
                Write-Host "      ... and $($cacheFiles.Count - 5) more files" -ForegroundColor Gray
            }
        }
    }
    else {
        $cacheResults[$cachePath] = @{
            Exists = $false
            FileCount = 0
            TotalSize = 0
            Files = @()
        }
        Write-Host "   ❌ Cache folder does not exist" -ForegroundColor Red
    }
}

# Get cache statistics from API
Write-Host "`n🌐 Getting cache statistics from API..." -ForegroundColor Yellow
try {
    $apiCacheStats = Invoke-RestMethod -Uri "$ApiBaseUrl/cache/statistics" -Method GET -TimeoutSec 30
    
    if ($apiCacheStats) {
        Write-Host "✅ API cache statistics retrieved" -ForegroundColor Green
        Write-Host "📊 API Cache Statistics:" -ForegroundColor Cyan
        Write-Host "   💾 Total Size: $([math]::Round($apiCacheStats.Summary.TotalSizeBytes / 1MB, 2)) MB" -ForegroundColor Gray
        Write-Host "   📁 Total Folders: $($apiCacheStats.Summary.TotalFolders)" -ForegroundColor Gray
        Write-Host "   🖼️ Total Images: $($apiCacheStats.Summary.TotalImages)" -ForegroundColor Gray
        
        if ($apiCacheStats.CacheFolders) {
            Write-Host "`n📂 Cache Folder Details from API:" -ForegroundColor Cyan
            foreach ($folder in $apiCacheStats.CacheFolders) {
                Write-Host "   📁 $($folder.Name): $($folder.Path)" -ForegroundColor Gray
                Write-Host "      💾 Size: $([math]::Round($folder.TotalSizeBytes / 1MB, 2)) MB" -ForegroundColor Gray
                Write-Host "      🖼️ Images: $($folder.ImageCount)" -ForegroundColor Gray
                Write-Host "      📊 Usage: $([math]::Round(($folder.TotalSizeBytes / $folder.MaxSizeBytes) * 100, 1))%" -ForegroundColor Gray
            }
        }
    }
}
catch {
    Write-Host "⚠️ Could not retrieve API cache statistics: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n📈 Cache Generation Summary:" -ForegroundColor Cyan
Write-Host "   📁 Total Cache Folders: $($CachePaths.Count)" -ForegroundColor Gray
Write-Host "   📄 Total Cache Files: $totalCacheFiles" -ForegroundColor Gray
Write-Host "   💾 Total Cache Size: $([math]::Round($totalCacheSize / 1MB, 2)) MB" -ForegroundColor Gray

# Check if cache generation is complete
$cacheFoldersWithFiles = ($cacheResults.Values | Where-Object { $_.FileCount -gt 0 }).Count
Write-Host "   ✅ Cache Folders with Files: $cacheFoldersWithFiles" -ForegroundColor Green

if ($totalCacheFiles -gt 0) {
    Write-Host "`n🎯 SUCCESS: Cache generation is working!" -ForegroundColor Green
    Write-Host "   📊 Average files per folder: $([math]::Round($totalCacheFiles / $CachePaths.Count, 1))" -ForegroundColor Gray
    Write-Host "   💾 Average size per folder: $([math]::Round($totalCacheSize / $CachePaths.Count / 1MB, 2)) MB" -ForegroundColor Gray
}
else {
    Write-Host "`n⚠️ WARNING: No cache files found in any cache folder" -ForegroundColor Yellow
    Write-Host "   🔄 Cache generation may still be in progress" -ForegroundColor Yellow
    Write-Host "   ⏳ Background jobs may need more time to complete" -ForegroundColor Yellow
}

# Check for specific collection cache files
Write-Host "`n🔍 Looking for collection-specific cache files..." -ForegroundColor Yellow
$collectionCacheFound = $false

foreach ($cachePath in $CachePaths) {
    if ($cacheResults[$cachePath].FileCount -gt 0) {
        $collectionDirs = Get-ChildItem -Path $cachePath -Directory | Where-Object { $_.Name -match "collection|geldoru" -or $_.Name -match "doom|evil|invincible|master|start" }
        
        if ($collectionDirs.Count -gt 0) {
            $collectionCacheFound = $true
            Write-Host "   📁 Found collection directories in $cachePath:" -ForegroundColor Cyan
            foreach ($dir in $collectionDirs) {
                $dirFiles = Get-ChildItem -Path $dir.FullName -File
                Write-Host "      📂 $($dir.Name): $($dirFiles.Count) files" -ForegroundColor Gray
            }
        }
    }
}

if ($collectionCacheFound) {
    Write-Host "✅ Collection-specific cache files found!" -ForegroundColor Green
}
else {
    Write-Host "⚠️ No collection-specific cache directories found" -ForegroundColor Yellow
}

return @{
    Success = $totalCacheFiles -gt 0
    TotalCacheFiles = $totalCacheFiles
    TotalCacheSize = $totalCacheSize
    CacheResults = $cacheResults
    ApiCacheStats = $apiCacheStats
    CollectionCacheFound = $collectionCacheFound
}
