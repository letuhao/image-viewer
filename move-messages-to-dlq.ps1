#!/usr/bin/env pwsh
# Move messages from collection.scan to DLQ before deleting the queue
# This ensures no message loss when changing queue TTL

param(
    [string]$QueueName = "collection.scan",
    [string]$DlqName = "imageviewer.dlq",
    [string]$RabbitMQHost = "localhost",
    [int]$RabbitMQPort = 15672,
    [string]$Username = "guest",
    [string]$Password = "guest"
)

Write-Host "🔄 Safe Message Migration Tool" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source Queue: $QueueName" -ForegroundColor Yellow
Write-Host "Target Queue: $DlqName" -ForegroundColor Yellow
Write-Host ""

# Create credentials
$securePassword = ConvertTo-SecureString $Password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($Username, $securePassword)

$baseUrl = "http://${RabbitMQHost}:${RabbitMQPort}/api"
$vhost = "%2F" # URL-encoded "/"

# Step 1: Check source queue
Write-Host "📊 Step 1: Checking source queue..." -ForegroundColor Yellow
try {
    $sourceQueueInfo = Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$QueueName" -Credential $credential -Method Get
    $messageCount = $sourceQueueInfo.messages
    Write-Host "   Found $messageCount messages in $QueueName" -ForegroundColor White
    
    if ($messageCount -eq 0) {
        Write-Host "✅ No messages to move. Queue is empty." -ForegroundColor Green
        exit 0
    }
} catch {
    Write-Host "❌ Failed to check source queue" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Check DLQ exists
Write-Host ""
Write-Host "📊 Step 2: Checking DLQ..." -ForegroundColor Yellow
try {
    $dlqInfo = Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$DlqName" -Credential $credential -Method Get
    Write-Host "   DLQ exists with $($dlqInfo.messages) messages" -ForegroundColor White
} catch {
    Write-Host "❌ DLQ does not exist!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Confirm migration
Write-Host ""
Write-Host "⚠️  WARNING: This will move $messageCount messages from $QueueName to $DlqName" -ForegroundColor Yellow
Write-Host "   The messages will be safe in DLQ and recovered by the Worker." -ForegroundColor Yellow
$confirm = Read-Host "Do you want to proceed? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "❌ Migration cancelled" -ForegroundColor Red
    exit 0
}

# Step 4: Move messages
Write-Host ""
Write-Host "🔄 Step 3: Moving messages..." -ForegroundColor Yellow

$movedCount = 0
$failedCount = 0

for ($i = 1; $i -le $messageCount; $i++) {
    Write-Host "   Processing message $i of $messageCount..." -ForegroundColor Gray
    
    try {
        # Get message from source queue (without auto-ack)
        $getBody = @{
            count = 1
            ackmode = "ack_requeue_false"  # Remove from source queue
            encoding = "auto"
        } | ConvertTo-Json
        
        $messages = Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$QueueName/get" `
                                      -Credential $credential `
                                      -Method Post `
                                      -ContentType "application/json" `
                                      -Body $getBody
        
        if ($null -eq $messages -or $messages.Count -eq 0) {
            Write-Host "   ⚠️  No more messages in queue" -ForegroundColor Yellow
            break
        }
        
        $message = $messages[0]
        
        # Prepare properties for DLQ
        $properties = $message.properties
        
        # Add x-moved-to-dlq header for tracking
        if ($null -eq $properties.headers) {
            $properties.headers = @{}
        }
        $properties.headers["x-moved-to-dlq"] = $true
        $properties.headers["x-moved-from"] = $QueueName
        $properties.headers["x-moved-at"] = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
        
        # Publish to DLQ
        $publishBody = @{
            properties = $properties
            payload = $message.payload
            payload_encoding = $message.payload_encoding
            routing_key = $DlqName
        } | ConvertTo-Json -Depth 10
        
        # Use default exchange (direct routing to queue by name)
        Invoke-RestMethod -Uri "$baseUrl/exchanges/$vhost/amq.default/publish" `
                          -Credential $credential `
                          -Method Post `
                          -ContentType "application/json" `
                          -Body $publishBody | Out-Null
        
        $movedCount++
        
    } catch {
        Write-Host "   ❌ Failed to move message $i" -ForegroundColor Red
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        $failedCount++
    }
}

# Step 5: Summary
Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "📊 MIGRATION SUMMARY" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Successfully moved: $movedCount messages" -ForegroundColor Green
if ($failedCount -gt 0) {
    Write-Host "❌ Failed to move: $failedCount messages" -ForegroundColor Red
}
Write-Host ""

# Step 6: Verify
Write-Host "📊 Verifying..." -ForegroundColor Yellow
try {
    $sourceQueueAfter = Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$QueueName" -Credential $credential -Method Get
    $dlqAfter = Invoke-RestMethod -Uri "$baseUrl/queues/$vhost/$DlqName" -Credential $credential -Method Get
    
    Write-Host "   Source queue ($QueueName): $($sourceQueueAfter.messages) messages remaining" -ForegroundColor White
    Write-Host "   DLQ ($DlqName): $($dlqAfter.messages) messages total" -ForegroundColor White
} catch {
    Write-Host "⚠️  Could not verify final counts" -ForegroundColor Yellow
}

Write-Host ""
if ($movedCount -eq $messageCount -and $failedCount -eq 0) {
    Write-Host "✅ All messages safely moved to DLQ!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Delete the '$QueueName' queue in RabbitMQ Management UI" -ForegroundColor White
    Write-Host "2. Restart the Worker (dotnet run)" -ForegroundColor White
    Write-Host "3. Worker will recreate queue with new 24-hour TTL" -ForegroundColor White
    Write-Host "4. DLQ recovery will process all messages automatically" -ForegroundColor White
} else {
    Write-Host "⚠️  Migration completed with issues." -ForegroundColor Yellow
    Write-Host "   Please check RabbitMQ Management UI to verify message counts." -ForegroundColor Yellow
}

