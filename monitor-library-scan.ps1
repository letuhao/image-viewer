#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Monitor library scan process and collection creation

.DESCRIPTION
    Monitors the full flow:
    1. Scheduled job creation
    2. Job execution by Scheduler
    3. RabbitMQ message publishing
    4. Worker processing
    5. Collection creation

.PARAMETER LibraryId
    The ID of the library to monitor

.EXAMPLE
    .\monitor-library-scan.ps1 -LibraryId "68ea86ef6912ca662238eb5c"
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$LibraryId = "68ea86ef6912ca662238eb5c",
    
    [Parameter(Mandatory=$false)]
    [string]$MongoConnectionString = "mongodb://localhost:27017",
    
    [Parameter(Mandatory=$false)]
    [string]$DatabaseName = "image_viewer",
    
    [Parameter(Mandatory=$false)]
    [int]$RefreshInterval = 5
)

# Colors
function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[SUCCESS] $msg" -ForegroundColor Green }
function Write-Warning($msg) { Write-Host "[WARNING] $msg" -ForegroundColor Yellow }
function Write-Error($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  Library Scan Monitor" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""
Write-Info "Library ID: $LibraryId"
Write-Info "Refresh Interval: ${RefreshInterval}s"
Write-Host ""

# MongoDB query function
function Query-Mongo {
    param(
        [string]$Collection,
        [string]$Query
    )
    
    $mongoCmd = "mongosh `"$MongoConnectionString/$DatabaseName`" --quiet --eval `"$Query`""
    return Invoke-Expression $mongoCmd
}

# Main monitoring loop
$iteration = 0
while ($true) {
    $iteration++
    Clear-Host
    
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "  Library Scan Monitor (Iteration: $iteration)" -ForegroundColor Cyan
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host ""
    
    # 1. Check Library
    Write-Host "[1] Library Status:" -ForegroundColor Yellow
    $libraryQuery = "db.libraries.findOne({_id: ObjectId('$LibraryId')}, {name: 1, path: 1, 'settings.autoScan': 1, 'statistics.totalCollections': 1})"
    $library = Query-Mongo -Collection "libraries" -Query $libraryQuery
    Write-Host $library
    Write-Host ""
    
    # 2. Check Scheduled Job
    Write-Host "[2] Scheduled Job:" -ForegroundColor Yellow
    $jobQuery = "db.scheduled_jobs.findOne({'parameters.LibraryId': '$LibraryId'}, {name: 1, isEnabled: 1, cronExpression: 1, lastRunAt: 1, nextRunAt: 1, runCount: 1, successCount: 1, failureCount: 1})"
    $job = Query-Mongo -Collection "scheduled_jobs" -Query $jobQuery
    
    if ($job) {
        Write-Success "Found scheduled job"
        Write-Host $job
    } else {
        Write-Warning "No scheduled job found yet (Scheduler may take up to 5 minutes to sync)"
    }
    Write-Host ""
    
    # 3. Check Job Runs
    Write-Host "[3] Recent Job Executions:" -ForegroundColor Yellow
    $runsQuery = "db.scheduled_job_runs.find({'result.libraryId': '$LibraryId'}).sort({startedAt: -1}).limit(3).toArray()"
    $runs = Query-Mongo -Collection "scheduled_job_runs" -Query $runsQuery
    
    if ($runs -and $runs -ne "[]") {
        Write-Success "Found job execution(s)"
        Write-Host $runs
    } else {
        Write-Warning "No job executions yet"
    }
    Write-Host ""
    
    # 4. Check Collections
    Write-Host "[4] Collections Created:" -ForegroundColor Yellow
    $collectionsQuery = "db.collections.find({libraryId: ObjectId('$LibraryId')}, {name: 1, path: 1, imageCount: 1, createdAt: 1}).toArray()"
    $collections = Query-Mongo -Collection "collections" -Query $collectionsQuery
    
    if ($collections -and $collections -ne "[]") {
        Write-Success "Collections found!"
        Write-Host $collections
    } else {
        Write-Warning "No collections created yet"
    }
    Write-Host ""
    
    # 5. Check Background Jobs (Worker activity)
    Write-Host "[5] Background Jobs (Worker):" -ForegroundColor Yellow
    $bgJobsQuery = "db.background_jobs.find({status: {`$in: ['pending', 'processing']}}).sort({createdAt: -1}).limit(5).toArray()"
    $bgJobs = Query-Mongo -Collection "background_jobs" -Query $bgJobsQuery
    
    if ($bgJobs -and $bgJobs -ne "[]") {
        Write-Info "Active background jobs:"
        Write-Host $bgJobs
    } else {
        Write-Host "  No active background jobs" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Summary
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Gray
    Write-Host "Next refresh in ${RefreshInterval}s..." -ForegroundColor Gray
    Write-Host ""
    
    Start-Sleep -Seconds $RefreshInterval
}

