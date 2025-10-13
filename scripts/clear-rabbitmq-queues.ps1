# Clear RabbitMQ Queues Script
# Removes existing queues so they can be recreated with new settings

param(
    [switch]$Force = $false
)

Write-Host "🗑️ RabbitMQ Queue Cleanup Script" -ForegroundColor Red
Write-Host "This will DELETE all existing queues and their messages!" -ForegroundColor Yellow

if (-not $Force) {
    $confirm = Read-Host "Are you sure you want to continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Host "❌ Operation cancelled" -ForegroundColor Green
        exit 0
    }
}

Write-Host "`n🔍 Checking RabbitMQ service..." -ForegroundColor Cyan

try {
    $rabbitmqStatus = Get-Service RabbitMQ
    if ($rabbitmqStatus.Status -ne "Running") {
        Write-Host "❌ RabbitMQ service is not running. Please start RabbitMQ first." -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ RabbitMQ service is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Could not check RabbitMQ service status" -ForegroundColor Red
    exit 1
}

Write-Host "`n🔌 Connecting to RabbitMQ Management API..." -ForegroundColor Cyan

# RabbitMQ Management API endpoints
$baseUrl = "http://localhost:15672/api"
$credentials = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("guest:guest"))

$headers = @{
    "Authorization" = "Basic $credentials"
    "Content-Type" = "application/json"
}

try {
    # Test connection
    $response = Invoke-RestMethod -Uri "$baseUrl/overview" -Headers $headers -Method Get
    Write-Host "✅ Connected to RabbitMQ Management API" -ForegroundColor Green
    Write-Host "📊 RabbitMQ Version: $($response.rabbitmq_version)" -ForegroundColor White
} catch {
    Write-Host "❌ Could not connect to RabbitMQ Management API" -ForegroundColor Red
    Write-Host "Please ensure RabbitMQ Management plugin is enabled" -ForegroundColor Yellow
    exit 1
}

Write-Host "`n📋 Getting list of queues..." -ForegroundColor Cyan

try {
    $queues = Invoke-RestMethod -Uri "$baseUrl/queues" -Headers $headers -Method Get
    $imageViewerQueues = $queues | Where-Object { $_.name -like "*collection*" -or $_.name -like "*thumbnail*" -or $_.name -like "*cache*" -or $_.name -like "*image*" -or $_.name -like "*bulk*" -or $_.name -like "*library*" }
    
    if ($imageViewerQueues.Count -eq 0) {
        Write-Host "ℹ️ No ImageViewer queues found" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "📋 Found $($imageViewerQueues.Count) ImageViewer queues:" -ForegroundColor White
    foreach ($queue in $imageViewerQueues) {
        $messageCount = $queue.messages
        $consumerCount = $queue.consumers
        Write-Host "  📦 $($queue.name): $messageCount messages, $consumerCount consumers" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ Could not retrieve queue list" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🗑️ Deleting queues..." -ForegroundColor Red

$deletedCount = 0
$failedCount = 0

foreach ($queue in $imageViewerQueues) {
    try {
        $queueName = [System.Web.HttpUtility]::UrlEncode($queue.name)
        $vhost = [System.Web.HttpUtility]::UrlEncode($queue.vhost)
        
        Write-Host "  🗑️ Deleting queue: $($queue.name)..." -ForegroundColor Yellow
        
        Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$queueName" -Headers $headers -Method Delete
        
        Write-Host "    ✅ Deleted: $($queue.name)" -ForegroundColor Green
        $deletedCount++
    } catch {
        Write-Host "    ❌ Failed to delete: $($queue.name) - $($_.Exception.Message)" -ForegroundColor Red
        $failedCount++
    }
}

Write-Host "`n📊 Cleanup Summary:" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✅ Successfully deleted: $deletedCount queues" -ForegroundColor Green
if ($failedCount -gt 0) {
    Write-Host "❌ Failed to delete: $failedCount queues" -ForegroundColor Red
}
Write-Host "📋 Total processed: $($imageViewerQueues.Count) queues" -ForegroundColor White

if ($deletedCount -gt 0) {
    Write-Host "`n🎉 Queue cleanup completed!" -ForegroundColor Green
    Write-Host "You can now restart your ImageViewer Worker to recreate queues with new settings." -ForegroundColor Green
    Write-Host "`n💡 Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Restart ImageViewer Worker" -ForegroundColor White
    Write-Host "  2. Queues will be recreated with 50M message capacity" -ForegroundColor White
    Write-Host "  3. Monitor queue creation in worker logs" -ForegroundColor White
} else {
    Write-Host "`n⚠️ No queues were deleted" -ForegroundColor Yellow
}

Write-Host "`n🔍 Current queue status:" -ForegroundColor Cyan
try {
    $remainingQueues = Invoke-RestMethod -Uri "$baseUrl/queues" -Headers $headers -Method Get
    $imageViewerRemaining = $remainingQueues | Where-Object { $_.name -like "*collection*" -or $_.name -like "*thumbnail*" -or $_.name -like "*cache*" -or $_.name -like "*image*" -or $_.name -like "*bulk*" -or $_.name -like "*library*" }
    
    if ($imageViewerRemaining.Count -eq 0) {
        Write-Host "✅ All ImageViewer queues have been cleared" -ForegroundColor Green
    } else {
        Write-Host "⚠️ $($imageViewerRemaining.Count) ImageViewer queues still exist:" -ForegroundColor Yellow
        foreach ($queue in $imageViewerRemaining) {
            Write-Host "  📦 $($queue.name)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ Could not check remaining queues" -ForegroundColor Red
}
