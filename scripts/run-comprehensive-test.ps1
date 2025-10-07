# Comprehensive Test Runner
# This script orchestrates all the testing components

param(
    [string]$SourcePath = "L:\EMedia\AI_Generated\Geldoru",
    [string[]]$CachePaths = @("I:\Image_Cache", "J:\Image_Cache", "K:\Image_Cache", "L:\Image_Cache"),
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [int]$ExpectedCollections = 15,
    [switch]$SkipServerStart = $false
)

Write-Host "🚀 Starting Comprehensive ImageViewer Test Suite" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Cyan

$testResults = @{
    StartTime = Get-Date
    Steps = @{}
    OverallSuccess = $false
}

# Step 0: Cleanup Old Collections (if not skipped)
Write-Host "`n📋 Step 0: Cleaning up old collections" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $cleanupResult = & "$PSScriptRoot\cleanup-old-collections.ps1" -ApiBaseUrl $ApiBaseUrl -SearchPattern "Geldoru" -Force
    $testResults.Steps["Cleanup"] = $cleanupResult
    
    if ($cleanupResult.Success) {
        Write-Host "✅ Collection cleanup completed" -ForegroundColor Green
        Write-Host "   🗑️ Deleted: $($cleanupResult.CollectionsDeleted) collections" -ForegroundColor Gray
    }
    else {
        Write-Host "⚠️ Collection cleanup had issues: $($cleanupResult.Message)" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error during cleanup: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["Cleanup"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Step 1: Start Server (if not skipped)
if (-not $SkipServerStart) {
    Write-Host "`n📋 Step 1: Starting Server in Silent Mode" -ForegroundColor Yellow
    Write-Host "-" * 40 -ForegroundColor Gray
    
    try {
        $serverResult = & "$PSScriptRoot\start-server-silent.ps1" -LogPath "logs" -MaxWaitTime 30
        $testResults.Steps["ServerStart"] = @{
            Success = $serverResult.ServerReady
            Result = $serverResult
        }
        
        if ($serverResult.ServerReady) {
            Write-Host "✅ Server started successfully" -ForegroundColor Green
        }
        else {
            Write-Host "❌ Server failed to start" -ForegroundColor Red
            return $testResults
        }
    }
    catch {
        Write-Host "❌ Error starting server: $($_.Exception.Message)" -ForegroundColor Red
        $testResults.Steps["ServerStart"] = @{ Success = $false; Error = $_.Exception.Message }
        return $testResults
    }
}
else {
    Write-Host "`n⏭️ Step 1: Skipping server start (using existing instance)" -ForegroundColor Yellow
    $testResults.Steps["ServerStart"] = @{ Success = $true; Skipped = $true }
}

# Step 2: Setup Cache Folders
Write-Host "`n📋 Step 2: Setting up Cache Folders" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $cacheResult = & "$PSScriptRoot\setup-cache-folders.ps1" -CachePaths $CachePaths -ApiBaseUrl $ApiBaseUrl
    $testResults.Steps["CacheSetup"] = $cacheResult
    
    if ($cacheResult.Success) {
        Write-Host "✅ Cache folders setup completed" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Cache folders setup failed" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error setting up cache folders: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["CacheSetup"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Step 3: Bulk Add Collections
Write-Host "`n📋 Step 3: Bulk Adding Collections" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $bulkResult = & "$PSScriptRoot\bulk-add-collections.ps1" -SourcePath $SourcePath -ApiBaseUrl $ApiBaseUrl -MaxWaitTime 300
    $testResults.Steps["BulkAdd"] = $bulkResult
    
    if ($bulkResult.Success) {
        Write-Host "✅ Bulk collection addition completed" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Bulk collection addition failed" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error in bulk collection addition: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["BulkAdd"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Step 4: Verify Collections
Write-Host "`n📋 Step 4: Verifying Collections" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $verifyResult = & "$PSScriptRoot\verify-collections.ps1" -SourcePath $SourcePath -ApiBaseUrl $ApiBaseUrl -ExpectedCount $ExpectedCollections
    $testResults.Steps["CollectionVerification"] = $verifyResult
    
    if ($verifyResult.Success) {
        Write-Host "✅ Collection verification completed" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Collection verification failed" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ Error verifying collections: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["CollectionVerification"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Step 5: Monitor Background Jobs
Write-Host "`n📋 Step 5: Monitoring Background Jobs" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $jobResult = & "$PSScriptRoot\monitor-background-jobs.ps1" -ApiBaseUrl $ApiBaseUrl -MaxWaitTime 300 -CheckInterval 10
    $testResults.Steps["JobMonitoring"] = $jobResult
    
    if ($jobResult.AllJobsComplete) {
        Write-Host "✅ All background jobs completed" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ Some background jobs may still be running" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error monitoring background jobs: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["JobMonitoring"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Step 6: Check Cache Generation
Write-Host "`n📋 Step 6: Checking Cache Generation" -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $cacheGenResult = & "$PSScriptRoot\check-cache-generation.ps1" -CachePaths $CachePaths -ApiBaseUrl $ApiBaseUrl -MaxWaitTime 300
    $testResults.Steps["CacheGeneration"] = $cacheGenResult
    
    if ($cacheGenResult.Success) {
        Write-Host "✅ Cache generation verification completed" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ Cache generation may still be in progress" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ Error checking cache generation: $($_.Exception.Message)" -ForegroundColor Red
    $testResults.Steps["CacheGeneration"] = @{ Success = $false; Error = $_.Exception.Message }
}

# Final Summary
$testResults.EndTime = Get-Date
$testResults.Duration = $testResults.EndTime - $testResults.StartTime

Write-Host "`n" + "=" * 60 -ForegroundColor Cyan
Write-Host "📊 COMPREHENSIVE TEST RESULTS SUMMARY" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Cyan

Write-Host "⏱️ Total Test Duration: $([math]::Round($testResults.Duration.TotalMinutes, 1)) minutes" -ForegroundColor Cyan

$successCount = 0
$totalSteps = $testResults.Steps.Count

foreach ($stepName in $testResults.Steps.Keys) {
    $step = $testResults.Steps[$stepName]
    $status = if ($step.Success) { "✅ PASS" } else { "❌ FAIL" }
    $successCount += if ($step.Success) { 1 } else { 0 }
    
    Write-Host "   $status $stepName" -ForegroundColor $(if ($step.Success) { "Green" } else { "Red" })
    
    if (-not $step.Success -and $step.Error) {
        Write-Host "      💬 Error: $($step.Error)" -ForegroundColor Red
    }
}

$overallSuccess = $successCount -eq $totalSteps
$testResults.OverallSuccess = $overallSuccess

Write-Host "`n🎯 Overall Result: $(if ($overallSuccess) { "✅ ALL TESTS PASSED" } else { "❌ SOME TESTS FAILED" })" -ForegroundColor $(if ($overallSuccess) { "Green" } else { "Red" })
Write-Host "📊 Success Rate: $successCount/$totalSteps ($([math]::Round(($successCount / $totalSteps) * 100, 1))%)" -ForegroundColor Cyan

# Detailed Results
if ($testResults.Steps["CollectionVerification"].Success) {
    $verify = $testResults.Steps["CollectionVerification"]
    Write-Host "`n📁 Collection Results:" -ForegroundColor Cyan
    Write-Host "   📊 Total Collections: $($verify.TotalCollections)" -ForegroundColor Gray
    Write-Host "   ✅ Matching Collections: $($verify.MatchingCollections)" -ForegroundColor Gray
    Write-Host "   🎯 Expected: $($verify.ExpectedCount)" -ForegroundColor Gray
}

if ($testResults.Steps["CacheGeneration"].Success) {
    $cache = $testResults.Steps["CacheGeneration"]
    Write-Host "`n💾 Cache Results:" -ForegroundColor Cyan
    Write-Host "   📄 Total Cache Files: $($cache.TotalCacheFiles)" -ForegroundColor Gray
    Write-Host "   💾 Total Cache Size: $([math]::Round($cache.TotalCacheSize / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "   📁 Collection Cache Found: $($cache.CollectionCacheFound)" -ForegroundColor Gray
}

Write-Host "`n🏁 Test Suite Completed!" -ForegroundColor Green

return $testResults
