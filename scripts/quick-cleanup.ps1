# Quick Cleanup Script
# Simple script to quickly clean up old collections before testing

param(
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [switch]$DryRun = $false
)

Write-Host "🧹 Quick Collection Cleanup" -ForegroundColor Green
Write-Host "=" * 40 -ForegroundColor Cyan

if ($DryRun) {
    Write-Host "🔍 DRY RUN MODE - No collections will be deleted" -ForegroundColor Yellow
}

# Run cleanup
$result = & "$PSScriptRoot\cleanup-old-collections.ps1" -ApiBaseUrl $ApiBaseUrl -SearchPattern "Geldoru" -DryRun:$DryRun -Force

# Display results
Write-Host "`n📊 Cleanup Results:" -ForegroundColor Cyan
Write-Host "   📋 Collections Found: $($result.CollectionsFound)" -ForegroundColor Gray
Write-Host "   🗑️ Collections Deleted: $($result.CollectionsDeleted)" -ForegroundColor Gray
Write-Host "   ❌ Failed Deletions: $($result.FailedDeletions)" -ForegroundColor Gray
Write-Host "   📄 Message: $($result.Message)" -ForegroundColor Gray

if ($result.Success) {
    Write-Host "`n✅ Cleanup completed successfully!" -ForegroundColor Green
}
else {
    Write-Host "`n❌ Cleanup had issues" -ForegroundColor Red
}

return $result
