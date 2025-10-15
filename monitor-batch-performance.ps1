# PowerShell script to monitor batch processing performance
# Monitors memory usage, processing speed, and system resources

param(
    [int]$IntervalSeconds = 5,
    [int]$DurationMinutes = 30
)

Write-Host "📊 ImageViewer Batch Processing Performance Monitor" -ForegroundColor Green
Write-Host "Monitoring every $IntervalSeconds seconds for $DurationMinutes minutes" -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Cyan

$startTime = Get-Date
$endTime = $startTime.AddMinutes($DurationMinutes)

# Performance counters
$previousTotalMemory = 0
$previousProcessCount = 0
$processingRates = @()

while ((Get-Date) -lt $endTime) {
    Clear-Host
    Write-Host "📊 ImageViewer Batch Processing Performance Monitor" -ForegroundColor Green
    Write-Host "Started: $($startTime.ToString('HH:mm:ss')) | Current: $(Get-Date -Format 'HH:mm:ss') | Duration: $([math]::Round(((Get-Date) - $startTime).TotalMinutes, 1)) min" -ForegroundColor Yellow
    Write-Host ""

    # Get .NET processes
    $dotnetProcesses = Get-Process -Name "dotnet", "ImageViewer.Worker" -ErrorAction SilentlyContinue
    $totalMemory = ($dotnetProcesses | Measure-Object -Property WorkingSet -Sum).Sum
    $totalMemoryMB = [math]::Round($totalMemory / 1MB, 2)
    
    # Calculate processing rate (if available)
    $currentProcessCount = $dotnetProcesses.Count
    $memoryChange = $totalMemoryMB - $previousTotalMemory
    $processChange = $currentProcessCount - $previousProcessCount
    
    Write-Host "🖥️ System Resources:" -ForegroundColor Cyan
    Write-Host "  Total Memory Usage: $totalMemoryMB MB" -ForegroundColor White
    Write-Host "  Memory Change: $([math]::Round($memoryChange, 2)) MB" -ForegroundColor $(if ($memoryChange -gt 0) { "Yellow" } else { "Green" })
    Write-Host "  Active .NET Processes: $currentProcessCount" -ForegroundColor White
    Write-Host "  CPU Usage: $([math]::Round((Get-Counter '\Processor(_Total)\% Processor Time').CounterSamples[0].CookedValue, 1))%" -ForegroundColor White
    Write-Host ""

    # Disk I/O monitoring
    $diskCounters = Get-Counter '\PhysicalDisk(_Total)\Disk Read Bytes/sec', '\PhysicalDisk(_Total)\Disk Write Bytes/sec' -ErrorAction SilentlyContinue
    if ($diskCounters) {
        $readRate = [math]::Round($diskCounters.CounterSamples[0].CookedValue / 1MB, 2)
        $writeRate = [math]::Round($diskCounters.CounterSamples[1].CookedValue / 1MB, 2)
        Write-Host "💾 Disk I/O:" -ForegroundColor Cyan
        Write-Host "  Read Rate: $readRate MB/s" -ForegroundColor White
        Write-Host "  Write Rate: $writeRate MB/s" -ForegroundColor White
        Write-Host ""
    }

    # RabbitMQ Queue monitoring (if available)
    try {
        $rabbitmqStatus = Invoke-RestMethod -Uri "http://localhost:15672/api/queues" -Headers @{Authorization="Basic "+[Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))} -ErrorAction SilentlyContinue
        
        if ($rabbitmqStatus) {
            $thumbnailQueue = $rabbitmqStatus | Where-Object { $_.name -eq "thumbnail.generation" }
            if ($thumbnailQueue) {
                Write-Host "🐰 RabbitMQ Queues:" -ForegroundColor Cyan
                Write-Host "  thumbnail.generation: $($thumbnailQueue.messages) messages" -ForegroundColor White
                Write-Host "  Consumers: $($thumbnailQueue.consumers)" -ForegroundColor White
                Write-Host "  Processing Rate: $([math]::Round($thumbnailQueue.messages_deliver_get_rate, 1)) msg/s" -ForegroundColor White
                Write-Host ""
            }
        }
    } catch {
        Write-Host "🐰 RabbitMQ: Unable to connect to management interface" -ForegroundColor Yellow
        Write-Host ""
    }

    # Memory optimization recommendations
    Write-Host "💡 Performance Recommendations:" -ForegroundColor Cyan
    if ($totalMemoryMB -gt 8000) {
        Write-Host "  ⚠️ High memory usage detected - consider reducing batch size" -ForegroundColor Yellow
    } elseif ($totalMemoryMB -lt 2000) {
        Write-Host "  ✅ Memory usage is optimal" -ForegroundColor Green
    }
    
    if ($readRate -gt 100 -or $writeRate -gt 100) {
        Write-Host "  ⚠️ High disk I/O detected - SSD recommended for better performance" -ForegroundColor Yellow
    } elseif ($readRate -lt 50 -and $writeRate -lt 50) {
        Write-Host "  ✅ Disk I/O is within optimal range" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "Press Ctrl+C to stop monitoring..." -ForegroundColor Gray

    $previousTotalMemory = $totalMemoryMB
    $previousProcessCount = $currentProcessCount

    Start-Sleep -Seconds $IntervalSeconds
}

Write-Host "✅ Monitoring completed" -ForegroundColor Green
