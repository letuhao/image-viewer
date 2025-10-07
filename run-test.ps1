# Main Test Runner - Easy to use script for running comprehensive tests
# This script runs the complete test suite for ImageViewer

param(
    [string]$SourcePath = "L:\EMedia\AI_Generated\Geldoru",
    [switch]$SkipCleanup = $false,
    [switch]$SkipServerStart = $false,
    [switch]$DryRun = $false
)

Write-Host "🚀 ImageViewer Comprehensive Test Suite" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Cyan

# Check if source path exists
if (-not (Test-Path $SourcePath)) {
    Write-Host "❌ Source path does not exist: $SourcePath" -ForegroundColor Red
    Write-Host "Please check the path and try again." -ForegroundColor Yellow
    exit 1
}

# Count ZIP files
$zipFiles = Get-ChildItem -Path $SourcePath -Filter "*.zip" -ErrorAction SilentlyContinue
Write-Host "📦 Found $($zipFiles.Count) ZIP files in source directory" -ForegroundColor Cyan

if ($zipFiles.Count -eq 0) {
    Write-Host "❌ No ZIP files found in source directory" -ForegroundColor Red
    exit 1
}

# Display test configuration
Write-Host "`n📋 Test Configuration:" -ForegroundColor Yellow
Write-Host "   📂 Source Path: $SourcePath" -ForegroundColor Gray
Write-Host "   📦 ZIP Files: $($zipFiles.Count)" -ForegroundColor Gray
Write-Host "   🧹 Skip Cleanup: $SkipCleanup" -ForegroundColor Gray
Write-Host "   🚀 Skip Server Start: $SkipServerStart" -ForegroundColor Gray
Write-Host "   🔍 Dry Run: $DryRun" -ForegroundColor Gray

# Run the comprehensive test
Write-Host "`n🎯 Starting comprehensive test..." -ForegroundColor Green

try {
    $testResult = & "scripts\run-comprehensive-test.ps1" -SourcePath $SourcePath -ExpectedCollections $zipFiles.Count -SkipServerStart:$SkipServerStart
    
    # Display final results
    Write-Host "`n" + "=" * 50 -ForegroundColor Cyan
    Write-Host "🏁 TEST SUITE COMPLETED" -ForegroundColor Green
    Write-Host "=" * 50 -ForegroundColor Cyan
    
    if ($testResult.OverallSuccess) {
        Write-Host "🎉 ALL TESTS PASSED!" -ForegroundColor Green
        Write-Host "✅ ImageViewer is working correctly" -ForegroundColor Green
    }
    else {
        Write-Host "⚠️ SOME TESTS FAILED" -ForegroundColor Yellow
        Write-Host "📋 Check the detailed results above" -ForegroundColor Yellow
    }
    
    Write-Host "`n📊 Test Summary:" -ForegroundColor Cyan
    Write-Host "   ⏱️ Duration: $([math]::Round($testResult.Duration.TotalMinutes, 1)) minutes" -ForegroundColor Gray
    Write-Host "   📋 Total Steps: $($testResult.Steps.Count)" -ForegroundColor Gray
    
    $successCount = ($testResult.Steps.Values | Where-Object { $_.Success }).Count
    Write-Host "   ✅ Successful Steps: $successCount" -ForegroundColor Green
    Write-Host "   ❌ Failed Steps: $($testResult.Steps.Count - $successCount)" -ForegroundColor Red
    
    # Exit with appropriate code
    if ($testResult.OverallSuccess) {
        exit 0
    }
    else {
        exit 1
    }
}
catch {
    Write-Host "`n❌ Test suite failed with error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "🔍 Check the error details above" -ForegroundColor Yellow
    exit 1
}
