# Verify Collections Created from Bulk Operation
# This script verifies that collections were created correctly and compares with source files

param(
    [string]$SourcePath = "L:\EMedia\AI_Generated\Geldoru",
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [int]$ExpectedCount = 15
)

Write-Host "🔍 Verifying collections created from bulk operation..." -ForegroundColor Green

# Get source ZIP files for comparison
Write-Host "📂 Getting source ZIP files from: $SourcePath" -ForegroundColor Yellow
$sourceFiles = Get-ChildItem -Path $SourcePath -Filter "*.zip" | Sort-Object Name
$sourceFileNames = $sourceFiles | ForEach-Object { [System.IO.Path]::GetFileNameWithoutExtension($_.Name) }

Write-Host "📋 Source files found: $($sourceFiles.Count)" -ForegroundColor Cyan
$sourceFileNames | ForEach-Object { Write-Host "   📄 $_" -ForegroundColor Gray }

# Get collections from API
Write-Host "`n🌐 Fetching collections from API..." -ForegroundColor Yellow
try {
    $collectionsResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/collections" -Method GET -TimeoutSec 30
    
    if ($collectionsResponse -and $collectionsResponse.Count -gt 0) {
        Write-Host "✅ Successfully retrieved collections from API" -ForegroundColor Green
        Write-Host "📊 Total collections in database: $($collectionsResponse.Count)" -ForegroundColor Cyan
        
        # Filter collections that match our source files
        $matchingCollections = @()
        $unmatchedCollections = @()
        $unmatchedSourceFiles = @()
        
        foreach ($collection in $collectionsResponse) {
            $collectionName = $collection.Name
            $found = $false
            
            foreach ($sourceName in $sourceFileNames) {
                if ($collectionName -like "*$sourceName*" -or $sourceName -like "*$collectionName*") {
                    $matchingCollections += $collection
                    $found = $true
                    break
                }
            }
            
            if (-not $found) {
                $unmatchedCollections += $collection
            }
        }
        
        # Find unmatched source files
        foreach ($sourceName in $sourceFileNames) {
            $found = $false
            foreach ($collection in $collectionsResponse) {
                if ($collection.Name -like "*$sourceName*" -or $sourceName -like "*$collection.Name*") {
                    $found = $true
                    break
                }
            }
            if (-not $found) {
                $unmatchedSourceFiles += $sourceName
            }
        }
        
        # Display results
        Write-Host "`n📈 Verification Results:" -ForegroundColor Cyan
        Write-Host "   ✅ Matching collections: $($matchingCollections.Count)" -ForegroundColor Green
        Write-Host "   ❓ Unmatched collections: $($unmatchedCollections.Count)" -ForegroundColor Yellow
        Write-Host "   ❓ Unmatched source files: $($unmatchedSourceFiles.Count)" -ForegroundColor Yellow
        
        # Show matching collections
        if ($matchingCollections.Count -gt 0) {
            Write-Host "`n✅ Successfully Created Collections:" -ForegroundColor Green
            foreach ($collection in $matchingCollections) {
                Write-Host "   📁 $($collection.Name)" -ForegroundColor Gray
                Write-Host "      🆔 ID: $($collection.Id)" -ForegroundColor Gray
                Write-Host "      📂 Path: $($collection.Path)" -ForegroundColor Gray
                Write-Host "      📊 Type: $($collection.Type)" -ForegroundColor Gray
                Write-Host "      📅 Created: $($collection.CreatedAt)" -ForegroundColor Gray
            }
        }
        
        # Show unmatched collections
        if ($unmatchedCollections.Count -gt 0) {
            Write-Host "`n❓ Collections not from this bulk operation:" -ForegroundColor Yellow
            foreach ($collection in $unmatchedCollections) {
                Write-Host "   📁 $($collection.Name)" -ForegroundColor Gray
            }
        }
        
        # Show unmatched source files
        if ($unmatchedSourceFiles.Count -gt 0) {
            Write-Host "`n❓ Source files not found as collections:" -ForegroundColor Yellow
            foreach ($sourceFile in $unmatchedSourceFiles) {
                Write-Host "   📄 $sourceFile" -ForegroundColor Gray
            }
        }
        
        # Check if we have the expected count
        if ($matchingCollections.Count -eq $ExpectedCount) {
            Write-Host "`n🎯 SUCCESS: Found exactly $ExpectedCount collections as expected!" -ForegroundColor Green
        }
        elseif ($matchingCollections.Count -gt $ExpectedCount) {
            Write-Host "`n⚠️ WARNING: Found more collections ($($matchingCollections.Count)) than expected ($ExpectedCount)" -ForegroundColor Yellow
        }
        else {
            Write-Host "`n❌ ERROR: Found fewer collections ($($matchingCollections.Count)) than expected ($ExpectedCount)" -ForegroundColor Red
        }
        
        # Get detailed collection information
        Write-Host "`n🔍 Getting detailed collection information..." -ForegroundColor Yellow
        foreach ($collection in $matchingCollections) {
            try {
                $detailResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/collections/$($collection.Id)" -Method GET -TimeoutSec 10
                if ($detailResponse) {
                    Write-Host "📊 Collection Details - $($collection.Name):" -ForegroundColor Cyan
                    Write-Host "   🖼️ Media Items: $($detailResponse.MediaItemCount)" -ForegroundColor Gray
                    Write-Host "   💾 Total Size: $($detailResponse.TotalSizeBytes) bytes" -ForegroundColor Gray
                    Write-Host "   📅 Last Modified: $($detailResponse.LastModifiedAt)" -ForegroundColor Gray
                }
            }
            catch {
                Write-Host "⚠️ Could not get details for collection: $($collection.Name)" -ForegroundColor Yellow
            }
        }
        
        return @{
            Success = $true
            TotalCollections = $collectionsResponse.Count
            MatchingCollections = $matchingCollections.Count
            ExpectedCount = $ExpectedCount
            Collections = $matchingCollections
            UnmatchedCollections = $unmatchedCollections
            UnmatchedSourceFiles = $unmatchedSourceFiles
        }
    }
    else {
        Write-Host "❌ No collections found in API response" -ForegroundColor Red
        return @{
            Success = $false
            Error = "No collections found"
        }
    }
}
catch {
    Write-Host "❌ Failed to fetch collections: $($_.Exception.Message)" -ForegroundColor Red
    return @{
        Success = $false
        Error = $_.Exception.Message
    }
}
