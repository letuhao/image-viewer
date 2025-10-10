#!/usr/bin/env pwsh
# Redis Cache Integration Test Script
# Tests thumbnail caching functionality and verifies cache hits/misses in logs

Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "🧪 REDIS CACHE INTEGRATION TEST" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# Configuration
$API_BASE_URL = "https://localhost:11001/api/v1"
$LOG_FILE = ".\logs\log-*.txt"

# Test parameters
$COLLECTION_ID = $null
$THUMBNAIL_ID = $null

# Step 1: Get a collection with thumbnails
Write-Host "📋 Step 1: Finding a collection with thumbnails..." -ForegroundColor Cyan

try {
    $collections = Invoke-RestMethod -Uri "$API_BASE_URL/collections?limit=10" -Method Get -SkipCertificateCheck
    
    foreach ($collection in $collections.data) {
        if ($collection.thumbnailCount -gt 0 -and $collection.hasThumbnail) {
            $COLLECTION_ID = $collection.id
            $THUMBNAIL_ID = $collection.thumbnailImageId
            Write-Host "   ✅ Found collection: $($collection.name)" -ForegroundColor Green
            Write-Host "      Collection ID: $COLLECTION_ID" -ForegroundColor White
            Write-Host "      Thumbnail ID: $THUMBNAIL_ID" -ForegroundColor White
            Write-Host "      Thumbnail Count: $($collection.thumbnailCount)" -ForegroundColor White
            break
        }
    }
    
    if (-not $COLLECTION_ID) {
        Write-Host "   ❌ No collection with thumbnails found!" -ForegroundColor Red
        Write-Host "   💡 Please scan a collection first to generate thumbnails." -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "   ❌ Failed to get collections: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Clear log file to see fresh logs
Write-Host "📋 Step 2: Preparing log monitoring..." -ForegroundColor Cyan
$logFiles = Get-ChildItem $LOG_FILE | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($logFiles) {
    Write-Host "   📄 Monitoring log file: $($logFiles.FullName)" -ForegroundColor White
} else {
    Write-Host "   ⚠️  No log file found, will create new one" -ForegroundColor Yellow
}
Write-Host ""

# Function to check logs for cache activity
function Get-CacheLogEntries {
    param(
        [string]$SearchTerm,
        [int]$LastNLines = 50
    )
    
    $logFiles = Get-ChildItem $LOG_FILE | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($logFiles) {
        $content = Get-Content $logFiles.FullName -Tail $LastNLines -ErrorAction SilentlyContinue
        return $content | Where-Object { $_ -match $SearchTerm }
    }
    return @()
}

# Function to make thumbnail request and measure time
function Test-ThumbnailRequest {
    param(
        [string]$CollectionId,
        [string]$ThumbnailId,
        [int]$RequestNumber
    )
    
    $url = "$API_BASE_URL/collections/$CollectionId/thumbnails/$ThumbnailId"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    
    try {
        $response = Invoke-WebRequest -Uri $url -Method Get -SkipCertificateCheck
        $stopwatch.Stop()
        
        return @{
            Success = $true
            StatusCode = $response.StatusCode
            ContentLength = $response.Content.Length
            ResponseTime = $stopwatch.ElapsedMilliseconds
            Request = $RequestNumber
        }
    } catch {
        $stopwatch.Stop()
        return @{
            Success = $false
            Error = $_.Exception.Message
            ResponseTime = $stopwatch.ElapsedMilliseconds
            Request = $RequestNumber
        }
    }
}

# Step 3: First request (should be CACHE MISS - load from disk)
Write-Host "📋 Step 3: First request (Expected: CACHE MISS - Load from disk)" -ForegroundColor Cyan
Start-Sleep -Seconds 1 # Brief pause to ensure clean logs

$result1 = Test-ThumbnailRequest -CollectionId $COLLECTION_ID -ThumbnailId $THUMBNAIL_ID -RequestNumber 1

if ($result1.Success) {
    Write-Host "   ✅ Request successful!" -ForegroundColor Green
    Write-Host "      Status Code: $($result1.StatusCode)" -ForegroundColor White
    Write-Host "      Content Size: $([Math]::Round($result1.ContentLength / 1024, 2)) KB" -ForegroundColor White
    Write-Host "      Response Time: $($result1.ResponseTime) ms" -ForegroundColor Yellow
} else {
    Write-Host "   ❌ Request failed: $($result1.Error)" -ForegroundColor Red
}

# Wait for logs to be written
Start-Sleep -Seconds 1

# Check logs for cache miss
$cacheMissLogs = Get-CacheLogEntries -SearchTerm "Cache MISS|not in cache|loading from disk"
if ($cacheMissLogs) {
    Write-Host "   📊 Log Analysis:" -ForegroundColor Cyan
    Write-Host "      ✅ CACHE MISS detected in logs (Info level)!" -ForegroundColor Green
    $cacheMissLogs | ForEach-Object {
        if ($_ -match $THUMBNAIL_ID) {
            Write-Host "      📝 $_" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "   ⚠️  No cache miss logs found (may already be cached)" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Second request (should be CACHE HIT - load from Redis)
Write-Host "📋 Step 4: Second request (Expected: CACHE HIT - Load from Redis)" -ForegroundColor Cyan
Start-Sleep -Seconds 1

$result2 = Test-ThumbnailRequest -CollectionId $COLLECTION_ID -ThumbnailId $THUMBNAIL_ID -RequestNumber 2

if ($result2.Success) {
    Write-Host "   ✅ Request successful!" -ForegroundColor Green
    Write-Host "      Status Code: $($result2.StatusCode)" -ForegroundColor White
    Write-Host "      Content Size: $([Math]::Round($result2.ContentLength / 1024, 2)) KB" -ForegroundColor White
    Write-Host "      Response Time: $($result2.ResponseTime) ms" -ForegroundColor Green
    
    # Calculate performance improvement
    if ($result1.ResponseTime -gt 0) {
        $improvement = [Math]::Round(($result1.ResponseTime - $result2.ResponseTime) / $result1.ResponseTime * 100, 1)
        $speedup = [Math]::Round($result1.ResponseTime / $result2.ResponseTime, 1)
        Write-Host "      🚀 Performance Improvement: $improvement% faster ($($speedup)x speedup)" -ForegroundColor Green
    }
} else {
    Write-Host "   ❌ Request failed: $($result2.Error)" -ForegroundColor Red
}

# Wait for logs to be written
Start-Sleep -Seconds 1

# Check logs for cache hit (Note: Cache HIT is at Debug level to avoid log bloat)
$cacheHitLogs = Get-CacheLogEntries -SearchTerm "Cache HIT|from Redis cache|Serving.*from.*cache"
if ($cacheHitLogs) {
    Write-Host "   📊 Log Analysis:" -ForegroundColor Cyan
    Write-Host "      ✅ CACHE HIT detected in logs (Debug level)!" -ForegroundColor Green
    $cacheHitLogs | ForEach-Object {
        if ($_ -match $THUMBNAIL_ID) {
            Write-Host "      📝 $_" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "   💡 No cache hit logs found (Debug level may be disabled)" -ForegroundColor Yellow
    Write-Host "      To see cache hits, set logging to Debug level in appsettings.json" -ForegroundColor Gray
}

Write-Host ""

# Step 5: Multiple requests to test consistency
Write-Host "📋 Step 5: Running 10 rapid requests to test cache consistency..." -ForegroundColor Cyan
$times = @()

for ($i = 3; $i -le 12; $i++) {
    $result = Test-ThumbnailRequest -CollectionId $COLLECTION_ID -ThumbnailId $THUMBNAIL_ID -RequestNumber $i
    if ($result.Success) {
        $times += $result.ResponseTime
        Write-Host "   Request $i : $($result.ResponseTime) ms" -ForegroundColor Gray
    }
    Start-Sleep -Milliseconds 100
}

if ($times.Count -gt 0) {
    $avgTime = [Math]::Round(($times | Measure-Object -Average).Average, 2)
    $minTime = ($times | Measure-Object -Minimum).Minimum
    $maxTime = ($times | Measure-Object -Maximum).Maximum
    
    Write-Host ""
    Write-Host "   📊 Statistics:" -ForegroundColor Cyan
    Write-Host "      Average Response Time: $avgTime ms" -ForegroundColor White
    Write-Host "      Min Response Time: $minTime ms" -ForegroundColor Green
    Write-Host "      Max Response Time: $maxTime ms" -ForegroundColor Yellow
}

Write-Host ""

# Step 6: Check Redis cache statistics (if endpoint exists)
Write-Host "📋 Step 6: Checking cache statistics..." -ForegroundColor Cyan

# Get recent cache-related logs
$recentCacheLogs = Get-CacheLogEntries -SearchTerm "Cache|Redis|Cached" -LastNLines 100

$hitCount = ($recentCacheLogs | Where-Object { $_ -match "Cache HIT|from Redis" }).Count
$missCount = ($recentCacheLogs | Where-Object { $_ -match "Cache MISS|not in cache" }).Count
$cacheStoreLogs = ($recentCacheLogs | Where-Object { $_ -match "Cached.*in Redis|SetCached" }).Count

Write-Host "   📊 Cache Activity Summary:" -ForegroundColor Cyan
Write-Host "      Cache Hits: $hitCount" -ForegroundColor Green
Write-Host "      Cache Misses: $missCount" -ForegroundColor Yellow
Write-Host "      Cache Stores: $cacheStoreLogs" -ForegroundColor White

if ($hitCount -gt 0 -and $missCount -gt 0) {
    $hitRate = [Math]::Round($hitCount / ($hitCount + $missCount) * 100, 1)
    Write-Host "      Hit Rate: $hitRate%" -ForegroundColor $(if ($hitRate -gt 80) { "Green" } else { "Yellow" })
}

Write-Host ""

# Final summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host "📊 TEST SUMMARY" -ForegroundColor Yellow
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan

Write-Host ""
Write-Host "🎯 Test Results:" -ForegroundColor Cyan

if ($result1.Success -and $result2.Success) {
    Write-Host "   ✅ Both requests successful" -ForegroundColor Green
    
    if ($result2.ResponseTime -lt $result1.ResponseTime) {
        Write-Host "   ✅ Second request faster than first (cache working!)" -ForegroundColor Green
        Write-Host "      First:  $($result1.ResponseTime) ms (disk)" -ForegroundColor White
        Write-Host "      Second: $($result2.ResponseTime) ms (Redis)" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Second request not faster (cache may not be working)" -ForegroundColor Yellow
    }
    
    if ($cacheHitLogs -and $cacheMissLogs) {
        Write-Host "   ✅ Cache HIT and MISS logs detected" -ForegroundColor Green
    } elseif ($cacheHitLogs) {
        Write-Host "   ✅ Cache HIT logs detected" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  No clear cache logs found" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🚀 Redis Caching: " -NoNewline
    if ($result2.ResponseTime -lt $result1.ResponseTime -and $cacheHitLogs) {
        Write-Host "WORKING! ✅" -ForegroundColor Green
    } else {
        Write-Host "NEEDS INVESTIGATION ⚠️" -ForegroundColor Yellow
    }
} else {
    Write-Host "   ❌ Test failed - check API logs for errors" -ForegroundColor Red
}

Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

# Return exit code based on test result
if ($result1.Success -and $result2.Success -and $result2.ResponseTime -lt $result1.ResponseTime) {
    exit 0
} else {
    exit 1
}

