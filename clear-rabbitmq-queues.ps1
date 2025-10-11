#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Clear RabbitMQ queues to resolve configuration conflicts

.DESCRIPTION
    This script deletes all ImageViewer queues from RabbitMQ to allow them to be
    recreated with updated configuration (e.g., x-max-length parameter changes).
    
    Use this when you see errors like:
    "PRECONDITION_FAILED - inequivalent arg 'x-max-length' for queue"

.EXAMPLE
    .\clear-rabbitmq-queues.ps1
#>

$ErrorActionPreference = "Continue"

# Configuration
$rabbitMQHost = "localhost"
$rabbitMQPort = 15672
$rabbitMQUser = "guest"
$rabbitMQPass = "guest"
$vhost = "/"

# Colors
$colors = @{
    Info = "Cyan"
    Success = "Green"
    Warning = "Yellow"
    Error = "Red"
}

function Write-Info($message) {
    Write-Host "[INFO] $message" -ForegroundColor $colors.Info
}

function Write-Success($message) {
    Write-Host "[SUCCESS] $message" -ForegroundColor $colors.Success
}

function Write-Warning($message) {
    Write-Host "[WARNING] $message" -ForegroundColor $colors.Warning
}

function Write-ErrorMsg($message) {
    Write-Host "[ERROR] $message" -ForegroundColor $colors.Error
}

# Create credentials
$base64Auth = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${rabbitMQUser}:${rabbitMQPass}"))
$headers = @{
    Authorization = "Basic $base64Auth"
}

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  RabbitMQ Queue Cleanup Tool" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# List of ImageViewer queues
$queuesToDelete = @(
    "collection.scan",
    "library_scan_queue",
    "image.processing",
    "cache.generation",
    "thumbnail.generation",
    "bulk.operation",
    "notification",
    "imageviewer.dlq"
)

try {
    Write-Info "Connecting to RabbitMQ Management API..."
    Write-Info "Host: ${rabbitMQHost}:${rabbitMQPort}"
    Write-Info "VHost: $vhost"
    Write-Host ""

    # Get all queues
    $vhostEncoded = [System.Uri]::EscapeDataString($vhost)
    $getQueuesUrl = "http://${rabbitMQHost}:${rabbitMQPort}/api/queues/${vhostEncoded}"
    
    $existingQueues = Invoke-RestMethod -Uri $getQueuesUrl -Headers $headers -Method Get
    
    Write-Info "Found $($existingQueues.Count) queues in RabbitMQ"
    Write-Host ""

    $deletedCount = 0
    $notFoundCount = 0

    foreach ($queueName in $queuesToDelete) {
        $queueExists = $existingQueues | Where-Object { $_.name -eq $queueName }
        
        if ($queueExists) {
            Write-Info "Deleting queue: $queueName"
            
            try {
                $queueNameEncoded = [System.Uri]::EscapeDataString($queueName)
                $deleteUrl = "http://${rabbitMQHost}:${rabbitMQPort}/api/queues/${vhostEncoded}/${queueNameEncoded}"
                
                Invoke-RestMethod -Uri $deleteUrl -Headers $headers -Method Delete | Out-Null
                Write-Success "  ✓ Deleted: $queueName"
                $deletedCount++
            }
            catch {
                Write-ErrorMsg "  ✗ Failed to delete $queueName : $($_.Exception.Message)"
            }
        }
        else {
            Write-Warning "  - Queue not found: $queueName (skipping)"
            $notFoundCount++
        }
    }

    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host "Summary:" -ForegroundColor Cyan
    Write-Success "  Deleted: $deletedCount queues"
    if ($notFoundCount -gt 0) {
        Write-Warning "  Not Found: $notFoundCount queues"
    }
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($deletedCount -gt 0) {
        Write-Success "✓ Queue cleanup complete!"
        Write-Info "You can now restart the Worker service."
        Write-Info "The queues will be recreated with the correct configuration."
    }
    else {
        Write-Warning "No queues were deleted."
    }
}
catch {
    Write-Host ""
    Write-ErrorMsg "Failed to connect to RabbitMQ Management API"
    Write-ErrorMsg $_.Exception.Message
    Write-Host ""
    Write-Warning "Make sure:"
    Write-Warning "  1. RabbitMQ is running"
    Write-Warning "  2. Management plugin is enabled: rabbitmq-plugins enable rabbitmq_management"
    Write-Warning "  3. Credentials are correct (default: guest/guest)"
    Write-Host ""
    exit 1
}

