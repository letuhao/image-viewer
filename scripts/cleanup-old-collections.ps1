# Cleanup Old Collections Script
# This script searches for and deletes old collections to prepare for clean testing

param(
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [string]$SearchPattern = "Geldoru",
    [switch]$DryRun = $false,
    [switch]$Force = $false
)

Write-Host "🧹 Cleaning up old collections for clean testing..." -ForegroundColor Green

# Step 1: Get all collections
Write-Host "`n🔍 Step 1: Searching for existing collections..." -ForegroundColor Yellow
Write-Host "-" * 40 -ForegroundColor Gray

try {
    $collectionsResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/collections" -Method GET -TimeoutSec 30
    
    if ($collectionsResponse -and $collectionsResponse.Count -gt 0) {
        Write-Host "📊 Found $($collectionsResponse.Count) total collections in database" -ForegroundColor Cyan
        
        # Filter collections matching our pattern
        $matchingCollections = $collectionsResponse | Where-Object { 
            $_.Name -like "*$SearchPattern*" -or 
            $_.Name -like "*Doom Breaker*" -or 
            $_.Name -like "*Evil God*" -or 
            $_.Name -like "*Invincible*" -or 
            $_.Name -like "*Master*" -or 
            $_.Name -like "*Stunning*" -or
            $_.Name -like "*AI Generated*" -or
            $_.Name -like "*Patreon*"
        }
        
        Write-Host "🎯 Found $($matchingCollections.Count) collections matching cleanup pattern" -ForegroundColor Cyan
        
        if ($matchingCollections.Count -eq 0) {
            Write-Host "✅ No collections found matching cleanup pattern - database is clean!" -ForegroundColor Green
            return @{
                Success = $true
                CollectionsFound = 0
                CollectionsDeleted = 0
                Message = "No cleanup needed"
            }
        }
        
        # Display collections to be deleted
        Write-Host "`n📋 Collections to be deleted:" -ForegroundColor Yellow
        foreach ($collection in $matchingCollections) {
            Write-Host "   📁 $($collection.Name)" -ForegroundColor Gray
            Write-Host "      🆔 ID: $($collection.Id)" -ForegroundColor Gray
            Write-Host "      📂 Path: $($collection.Path)" -ForegroundColor Gray
            Write-Host "      📅 Created: $($collection.CreatedAt)" -ForegroundColor Gray
        }
        
        # Confirmation (unless Force is used)
        if (-not $Force -and -not $DryRun) {
            Write-Host "`n⚠️ WARNING: This will permanently delete $($matchingCollections.Count) collections!" -ForegroundColor Red
            $confirmation = Read-Host "Are you sure you want to continue? (yes/no)"
            
            if ($confirmation -ne "yes") {
                Write-Host "❌ Operation cancelled by user" -ForegroundColor Yellow
                return @{
                    Success = $false
                    CollectionsFound = $matchingCollections.Count
                    CollectionsDeleted = 0
                    Message = "Cancelled by user"
                }
            }
        }
        
        # Delete collections
        $deletedCount = 0
        $failedDeletions = @()
        
        Write-Host "`n🗑️ Step 2: Deleting collections..." -ForegroundColor Yellow
        Write-Host "-" * 40 -ForegroundColor Gray
        
        foreach ($collection in $matchingCollections) {
            try {
                if ($DryRun) {
                    Write-Host "   🔍 [DRY RUN] Would delete: $($collection.Name)" -ForegroundColor Cyan
                    $deletedCount++
                }
                else {
                    Write-Host "   🗑️ Deleting: $($collection.Name)..." -ForegroundColor Yellow
                    
                    # Delete the collection
                    $deleteResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/collections/$($collection.Id)" -Method DELETE -TimeoutSec 30
                    
                    if ($deleteResponse) {
                        Write-Host "   ✅ Successfully deleted: $($collection.Name)" -ForegroundColor Green
                        $deletedCount++
                    }
                    else {
                        Write-Host "   ❌ Failed to delete: $($collection.Name)" -ForegroundColor Red
                        $failedDeletions += $collection
                    }
                }
            }
            catch {
                Write-Host "   ❌ Error deleting $($collection.Name): $($_.Exception.Message)" -ForegroundColor Red
                $failedDeletions += $collection
            }
        }
        
        # Summary
        Write-Host "`n📊 Cleanup Summary:" -ForegroundColor Cyan
        Write-Host "   📋 Collections Found: $($matchingCollections.Count)" -ForegroundColor Gray
        Write-Host "   ✅ Successfully Deleted: $deletedCount" -ForegroundColor Green
        Write-Host "   ❌ Failed Deletions: $($failedDeletions.Count)" -ForegroundColor Red
        
        if ($failedDeletions.Count -gt 0) {
            Write-Host "`n❌ Failed Deletions:" -ForegroundColor Red
            foreach ($failed in $failedDeletions) {
                Write-Host "   📁 $($failed.Name)" -ForegroundColor Red
            }
        }
        
        # Verify cleanup
        Write-Host "`n🔍 Step 3: Verifying cleanup..." -ForegroundColor Yellow
        Write-Host "-" * 40 -ForegroundColor Gray
        
        Start-Sleep -Seconds 2  # Give the API time to process
        
        try {
            $verifyResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/collections" -Method GET -TimeoutSec 30
            $remainingMatching = $verifyResponse | Where-Object { 
                $_.Name -like "*$SearchPattern*" -or 
                $_.Name -like "*Doom Breaker*" -or 
                $_.Name -like "*Evil God*" -or 
                $_.Name -like "*Invincible*" -or 
                $_.Name -like "*Master*" -or 
                $_.Name -like "*Stunning*" -or
                $_.Name -like "*AI Generated*" -or
                $_.Name -like "*Patreon*"
            }
            
            if ($remainingMatching.Count -eq 0) {
                Write-Host "✅ Cleanup verification successful - no matching collections remain!" -ForegroundColor Green
            }
            else {
                Write-Host "⚠️ $($remainingMatching.Count) matching collections still remain:" -ForegroundColor Yellow
                foreach ($remaining in $remainingMatching) {
                    Write-Host "   📁 $($remaining.Name)" -ForegroundColor Yellow
                }
            }
        }
        catch {
            Write-Host "⚠️ Could not verify cleanup: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
        return @{
            Success = $failedDeletions.Count -eq 0
            CollectionsFound = $matchingCollections.Count
            CollectionsDeleted = $deletedCount
            FailedDeletions = $failedDeletions.Count
            RemainingCollections = $remainingMatching.Count
            Message = if ($DryRun) { "Dry run completed" } else { "Cleanup completed" }
        }
    }
    else {
        Write-Host "📋 No collections found in database" -ForegroundColor Yellow
        return @{
            Success = $true
            CollectionsFound = 0
            CollectionsDeleted = 0
            Message = "No collections to clean up"
        }
    }
}
catch {
    Write-Host "❌ Error during cleanup: $($_.Exception.Message)" -ForegroundColor Red
    return @{
        Success = $false
        CollectionsFound = 0
        CollectionsDeleted = 0
        Error = $_.Exception.Message
    }
}
