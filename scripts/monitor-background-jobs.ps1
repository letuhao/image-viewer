# Monitor Background Jobs and System Status
# This script monitors background job completion and system health

param(
    [string]$ApiBaseUrl = "http://localhost:11000/api/v1",
    [int]$MaxWaitTime = 300,
    [int]$CheckInterval = 10
)

Write-Host "⚙️ Monitoring background jobs and system status..." -ForegroundColor Green

$startTime = Get-Date
$allJobsComplete = $false
$jobStatuses = @{}

while (-not $allJobsComplete -and ((Get-Date) - $startTime).TotalSeconds -lt $MaxWaitTime) {
    Write-Host "`n🔄 Checking job status... (Elapsed: $([math]::Round(((Get-Date) - $startTime).TotalSeconds, 0))s)" -ForegroundColor Yellow
    
    try {
        # Get all background jobs
        $jobsResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/jobs" -Method GET -TimeoutSec 10
        
        if ($jobsResponse -and $jobsResponse.Count -gt 0) {
            $activeJobs = $jobsResponse | Where-Object { $_.Status -in @("Pending", "Running", "Processing") }
            $completedJobs = $jobsResponse | Where-Object { $_.Status -eq "Completed" }
            $failedJobs = $jobsResponse | Where-Object { $_.Status -eq "Failed" }
            
            Write-Host "📊 Job Status Summary:" -ForegroundColor Cyan
            Write-Host "   ⏳ Active: $($activeJobs.Count)" -ForegroundColor Yellow
            Write-Host "   ✅ Completed: $($completedJobs.Count)" -ForegroundColor Green
            Write-Host "   ❌ Failed: $($failedJobs.Count)" -ForegroundColor Red
            Write-Host "   📋 Total: $($jobsResponse.Count)" -ForegroundColor Gray
            
            # Show active jobs details
            if ($activeJobs.Count -gt 0) {
                Write-Host "`n⏳ Active Jobs:" -ForegroundColor Yellow
                foreach ($job in $activeJobs) {
                    $progress = if ($job.TotalItems -gt 0) { [math]::Round(($job.ProcessedItems / $job.TotalItems) * 100, 1) } else { 0 }
                    Write-Host "   🔄 $($job.Type): $($job.Status) ($progress%)" -ForegroundColor Gray
                    Write-Host "      📊 Processed: $($job.ProcessedItems)/$($job.TotalItems)" -ForegroundColor Gray
                    Write-Host "      ⏱️ Started: $($job.StartedAt)" -ForegroundColor Gray
                }
            }
            
            # Show failed jobs
            if ($failedJobs.Count -gt 0) {
                Write-Host "`n❌ Failed Jobs:" -ForegroundColor Red
                foreach ($job in $failedJobs) {
                    Write-Host "   ❌ $($job.Type): $($job.Status)" -ForegroundColor Red
                    Write-Host "      📊 Processed: $($job.ProcessedItems)/$($job.TotalItems)" -ForegroundColor Gray
                    Write-Host "      ⏱️ Failed: $($job.CompletedAt)" -ForegroundColor Gray
                    if ($job.ErrorMessage) {
                        Write-Host "      💬 Error: $($job.ErrorMessage)" -ForegroundColor Red
                    }
                }
            }
            
            # Check if all jobs are complete
            if ($activeJobs.Count -eq 0) {
                $allJobsComplete = $true
                Write-Host "`n🎯 All background jobs completed!" -ForegroundColor Green
                
                if ($failedJobs.Count -eq 0) {
                    Write-Host "✅ No failed jobs - all operations successful!" -ForegroundColor Green
                }
                else {
                    Write-Host "⚠️ Some jobs failed - check details above" -ForegroundColor Yellow
                }
            }
        }
        else {
            Write-Host "📋 No background jobs found" -ForegroundColor Yellow
            $allJobsComplete = $true
        }
        
        # Check system health
        Write-Host "`n🏥 Checking system health..." -ForegroundColor Yellow
        try {
            $healthResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/../health" -Method GET -TimeoutSec 5
            Write-Host "✅ System health check passed" -ForegroundColor Green
        }
        catch {
            Write-Host "⚠️ System health check failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
        # Check performance metrics
        Write-Host "`n📊 Checking performance metrics..." -ForegroundColor Yellow
        try {
            $perfResponse = Invoke-RestMethod -Uri "$ApiBaseUrl/performance/metrics" -Method GET -TimeoutSec 10
            if ($perfResponse) {
                Write-Host "✅ Performance metrics retrieved" -ForegroundColor Green
                Write-Host "   💾 Memory Usage: $([math]::Round($perfResponse.MemoryUsageMB, 1)) MB" -ForegroundColor Gray
                Write-Host "   ⏱️ Average Response Time: $($perfResponse.AverageResponseTimeMs) ms" -ForegroundColor Gray
                Write-Host "   🔄 Active Connections: $($perfResponse.ActiveConnections)" -ForegroundColor Gray
            }
        }
        catch {
            Write-Host "⚠️ Could not retrieve performance metrics: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
    }
    catch {
        Write-Host "❌ Error checking job status: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    if (-not $allJobsComplete) {
        Write-Host "⏳ Waiting $CheckInterval seconds before next check..." -ForegroundColor Gray
        Start-Sleep -Seconds $CheckInterval
    }
}

if (-not $allJobsComplete) {
    Write-Host "`n⏰ Monitoring timeout reached ($MaxWaitTime seconds)" -ForegroundColor Yellow
    Write-Host "🔄 Some jobs may still be running in background" -ForegroundColor Yellow
}

# Final summary
$elapsedTime = [math]::Round(((Get-Date) - $startTime).TotalSeconds, 0)
Write-Host "`n📈 Monitoring Summary:" -ForegroundColor Cyan
Write-Host "   ⏱️ Total monitoring time: $elapsedTime seconds" -ForegroundColor Gray
Write-Host "   ✅ Jobs completed: $($allJobsComplete)" -ForegroundColor Gray

return @{
    AllJobsComplete = $allJobsComplete
    MonitoringTime = $elapsedTime
    JobStatuses = $jobStatuses
}
