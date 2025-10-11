#!/usr/bin/env pwsh
# Cleanup orphaned scheduled jobs from MongoDB

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Cleanup Orphaned Scheduled Jobs" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# MongoDB connection settings
$mongoHost = "localhost"
$mongoPort = "27017"
$database = "image_viewer"
$collection = "scheduled_jobs"

Write-Host "[INFO] Connecting to MongoDB..." -ForegroundColor Yellow
Write-Host "  Host: $mongoHost`:$mongoPort" -ForegroundColor Gray
Write-Host "  Database: $database" -ForegroundColor Gray
Write-Host "  Collection: $collection" -ForegroundColor Gray
Write-Host ""

try {
    # Find all orphaned jobs (hangfireJobId is null or empty)
    Write-Host "[INFO] Finding orphaned jobs (hangfireJobId = null)..." -ForegroundColor Yellow
    
    $findQuery = '{ "hangfireJobId": { "$in": [null, ""] } }'
    $findResult = mongosh --quiet --host $mongoHost --port $mongoPort $database --eval "db.$collection.find($findQuery).toArray()" | ConvertFrom-Json
    
    if ($findResult.Count -eq 0) {
        Write-Host "[SUCCESS] No orphaned jobs found!" -ForegroundColor Green
        Write-Host ""
        exit 0
    }
    
    Write-Host "[INFO] Found $($findResult.Count) orphaned job(s):" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($job in $findResult) {
        Write-Host "  • Name: $($job.name)" -ForegroundColor White
        Write-Host "    ID: $($job._id.'$oid')" -ForegroundColor Gray
        Write-Host "    Type: $($job.jobType)" -ForegroundColor Gray
        Write-Host "    Cron: $($job.cronExpression)" -ForegroundColor Gray
        Write-Host "    Created: $($job.createdAt.'$date')" -ForegroundColor Gray
        
        if ($job.parameters.LibraryId) {
            $libId = if ($job.parameters.LibraryId -is [string]) { 
                $job.parameters.LibraryId 
            } elseif ($job.parameters.LibraryId.'$oid') {
                $job.parameters.LibraryId.'$oid'
            } else {
                "Unknown"
            }
            Write-Host "    LibraryId: $libId" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    # Ask for confirmation
    $confirm = Read-Host "Do you want to delete these orphaned jobs? (yes/no)"
    
    if ($confirm -ne "yes") {
        Write-Host "[INFO] Cleanup cancelled by user" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host ""
    Write-Host "[INFO] Deleting orphaned jobs..." -ForegroundColor Yellow
    
    # Delete orphaned jobs
    $deleteQuery = '{ "hangfireJobId": { "$in": [null, ""] } }'
    $deleteResult = mongosh --quiet --host $mongoHost --port $mongoPort $database --eval "db.$collection.deleteMany($deleteQuery)"
    
    Write-Host "[SUCCESS] Deleted orphaned jobs" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " Cleanup Complete" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Refresh the Libraries page" -ForegroundColor White
    Write-Host "2. Delete and recreate libraries with AutoScan enabled" -ForegroundColor White
    Write-Host "3. New jobs will have proper LibraryId and Hangfire binding" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "[ERROR] Failed to cleanup orphaned jobs: $_" -ForegroundColor Red
    exit 1
}

