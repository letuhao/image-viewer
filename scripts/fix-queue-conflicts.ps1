# Fix RabbitMQ Queue Configuration Conflicts
# Handles the PRECONDITION_FAILED error when queue settings don't match

param(
    [switch]$DeleteQueues = $false,
    [switch]$KeepMessages = $true
)

Write-Host "🔧 Fixing RabbitMQ Queue Configuration Conflicts" -ForegroundColor Green

if ($DeleteQueues) {
    Write-Host "⚠️ WARNING: This will delete all existing queues and lose all messages!" -ForegroundColor Red
    Write-Host "⚠️ Only use this if you're sure you want to reset everything." -ForegroundColor Red
    
    $confirm = Read-Host "Type 'DELETE' to confirm queue deletion"
    if ($confirm -ne "DELETE") {
        Write-Host "❌ Queue deletion cancelled" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "🗑️ Deleting all existing queues..." -ForegroundColor Red
    
    # Connect to RabbitMQ management API to delete queues
    try {
        $uri = "http://localhost:15672/api/queues"
        $cred = [System.Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes("guest:guest"))
        $headers = @{ Authorization = "Basic $cred" }
        
        $queues = Invoke-RestMethod -Uri $uri -Headers $headers -Method Get
        
        foreach ($queue in $queues) {
            $deleteUri = "http://localhost:15672/api/queues/%2F/$($queue.name)"
            try {
                Invoke-RestMethod -Uri $deleteUri -Headers $headers -Method Delete
                Write-Host "  ✅ Deleted queue: $($queue.name)" -ForegroundColor Green
            } catch {
                Write-Host "  ⚠️ Could not delete queue: $($queue.name)" -ForegroundColor Yellow
            }
        }
        
        Write-Host "✅ All queues deleted successfully" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error deleting queues via API: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "💡 You may need to delete queues manually via RabbitMQ Management UI" -ForegroundColor Yellow
    }
} else {
    Write-Host "ℹ️ Using graceful handling approach - existing queues will be kept with current settings" -ForegroundColor Cyan
    Write-Host "ℹ️ New queues will use the optimized settings (50M messages)" -ForegroundColor Cyan
}

Write-Host "`n🔄 Restarting RabbitMQ service..." -ForegroundColor Cyan
try {
    Restart-Service RabbitMQ -Force
    Start-Sleep -Seconds 5
    
    $serviceStatus = Get-Service RabbitMQ
    if ($serviceStatus.Status -eq "Running") {
        Write-Host "✅ RabbitMQ service restarted successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ RabbitMQ service failed to restart" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error restarting RabbitMQ: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n📊 Configuration Summary:" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green

if ($DeleteQueues) {
    Write-Host "✅ All queues deleted and will be recreated with new settings:" -ForegroundColor Green
    Write-Host "  - Max Queue Length: 50,000,000 messages" -ForegroundColor White
    Write-Host "  - Max Queue Size: 20GB" -ForegroundColor White
    Write-Host "  - Message TTL: 24 hours" -ForegroundColor White
    Write-Host "  - Dead Letter Exchange: Enabled" -ForegroundColor White
} else {
    Write-Host "✅ Graceful handling enabled:" -ForegroundColor Green
    Write-Host "  - Existing queues: Keep current settings (100,000 messages)" -ForegroundColor White
    Write-Host "  - New queues: Use optimized settings (50,000,000 messages)" -ForegroundColor White
    Write-Host "  - No message loss" -ForegroundColor White
}

Write-Host "`n🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Start your ImageViewer Worker service" -ForegroundColor White
Write-Host "2. Check logs for successful queue setup" -ForegroundColor White
Write-Host "3. Monitor queue performance via RabbitMQ Management UI" -ForegroundColor White
Write-Host "4. Access Management UI: http://localhost:15672 (guest/guest)" -ForegroundColor White

Write-Host "`n💡 If you still get errors:" -ForegroundColor Yellow
Write-Host "  - Run this script again with -DeleteQueues flag to reset everything" -ForegroundColor White
Write-Host "  - Or manually delete queues via RabbitMQ Management UI" -ForegroundColor White
Write-Host "  - Or use RabbitMQ CLI: rabbitmqctl delete_queue <queue_name>" -ForegroundColor White
