# Setup Cache Folders via API
# This script creates cache folders using the ImageViewer API

param(
    [string[]]$CachePaths = @("I:\Image_Cache", "J:\Image_Cache", "K:\Image_Cache", "L:\Image_Cache"),
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [long]$MaxSizeBytes = 10737418240,  # 10GB
    [switch]$UseHttps = $false
)

Write-Host "📁 Setting up cache folders via API..." -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Cyan

# Determine the correct protocol
$protocol = if ($UseHttps) { "https" } else { "http" }
$port = if ($UseHttps) { "11001" } else { "11000" }
$fullApiUrl = "$protocol`://localhost:$port/api"

Write-Host "🌐 API Endpoint: $fullApiUrl" -ForegroundColor Yellow

# Test API connectivity first
Write-Host "🔍 Testing API connectivity..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$fullApiUrl/health" -Method GET -TimeoutSec 5 -SkipCertificateCheck:$UseHttps
    Write-Host "✅ API is accessible" -ForegroundColor Green
}
catch {
    Write-Host "❌ API connectivity test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure the ImageViewer server is running" -ForegroundColor Yellow
    exit 1
}

# Get existing cache folders
Write-Host "📋 Checking existing cache folders..." -ForegroundColor Yellow
try {
    $existingFolders = Invoke-RestMethod -Uri "$fullApiUrl/cache/folders" -Method GET -TimeoutSec 10 -SkipCertificateCheck:$UseHttps
    Write-Host "📊 Found $($existingFolders.Count) existing cache folders" -ForegroundColor Cyan
    
    if ($existingFolders.Count -gt 0) {
        Write-Host "📋 Existing folders:" -ForegroundColor Cyan
        foreach ($folder in $existingFolders) {
            Write-Host "   📁 $($folder.Name) - $($folder.Path)" -ForegroundColor Gray
        }
    }
}
catch {
    Write-Host "⚠️ Could not retrieve existing folders: $($_.Exception.Message)" -ForegroundColor Yellow
    $existingFolders = @()
}

# Create cache folders
Write-Host "🚀 Creating cache folders..." -ForegroundColor Green
$createdCount = 0
$failedCount = 0

foreach ($i in 0..($CachePaths.Length - 1)) {
    $path = $CachePaths[$i]
    $folderName = "Cache_$([char](65 + $i))"  # Cache_A, Cache_B, etc.
    $priority = $i + 1
    
    Write-Host "📁 Creating: $folderName at $path" -ForegroundColor Yellow
    
    # Check if folder already exists
    $exists = $existingFolders | Where-Object { $_.Path -eq $path -or $_.Name -eq $folderName }
    if ($exists) {
        Write-Host "   ⚠️ Folder already exists, skipping..." -ForegroundColor Yellow
        continue
    }
    
    # Create folder object
    $folderData = @{
        Name = $folderName
        Path = $path
        Priority = $priority
        MaxSize = $MaxSizeBytes
    }
    
    try {
        $response = Invoke-RestMethod -Uri "$fullApiUrl/cache/folders" -Method POST -Body ($folderData | ConvertTo-Json) -ContentType "application/json" -TimeoutSec 10 -SkipCertificateCheck:$UseHttps
        Write-Host "   ✅ Created: $($response.Name) (ID: $($response.Id))" -ForegroundColor Green
        $createdCount++
    }
    catch {
        Write-Host "   ❌ Failed: $($_.Exception.Message)" -ForegroundColor Red
        $failedCount++
    }
}

# Verify setup
Write-Host "🔍 Verifying cache folder setup..." -ForegroundColor Yellow
try {
    $finalFolders = Invoke-RestMethod -Uri "$fullApiUrl/cache/folders" -Method GET -TimeoutSec 10 -SkipCertificateCheck:$UseHttps
    Write-Host "📊 Total cache folders: $($finalFolders.Count)" -ForegroundColor Cyan
    
    if ($finalFolders.Count -gt 0) {
        Write-Host "📋 Final cache folders:" -ForegroundColor Cyan
        foreach ($folder in $finalFolders) {
            $sizeGB = [math]::Round($folder.MaxSize / 1GB, 2)
            Write-Host "   📁 $($folder.Name) - $($folder.Path) (Max: ${sizeGB}GB, Priority: $($folder.Priority))" -ForegroundColor Green
        }
    }
}
catch {
    Write-Host "❌ Failed to verify setup: $($_.Exception.Message)" -ForegroundColor Red
}

# Summary
Write-Host "=" * 50 -ForegroundColor Cyan
Write-Host "📊 Setup Summary:" -ForegroundColor Green
Write-Host "   ✅ Created: $createdCount folders" -ForegroundColor Green
Write-Host "   ❌ Failed: $failedCount folders" -ForegroundColor Red
Write-Host "   📁 Total: $($finalFolders.Count) folders" -ForegroundColor Cyan

if ($createdCount -gt 0) {
    Write-Host "🎉 Cache folder setup completed successfully!" -ForegroundColor Green
} else {
    Write-Host "⚠️ No new folders were created" -ForegroundColor Yellow
}

# Test cache statistics
Write-Host "📊 Testing cache statistics endpoint..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$fullApiUrl/cache/statistics" -Method GET -TimeoutSec 10 -SkipCertificateCheck:$UseHttps
    Write-Host "✅ Cache statistics retrieved successfully" -ForegroundColor Green
    Write-Host "   📊 Total folders: $($stats.Summary.TotalFolders)" -ForegroundColor Cyan
    Write-Host "   💾 Total capacity: $([math]::Round($stats.Summary.TotalCapacityBytes / 1GB, 2))GB" -ForegroundColor Cyan
}
catch {
    Write-Host "⚠️ Could not retrieve cache statistics: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "🏁 Cache folder setup script completed" -ForegroundColor Green
