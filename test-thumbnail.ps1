# Test Thumbnail Generation
# This script manually publishes a thumbnail message to RabbitMQ to test the consumer

Write-Host "=== Testing Thumbnail Generation ===" -ForegroundColor Cyan

# Test message
$message = @{
    imageId = "68e7d5182309ecdb2bcce263"
    collectionId = "68e7d40e2309ecdb2bbc089b"
    imagePath = "L:\EMedia\AI_Generated\Geldoru\[Geldoru] The Start of a Stunning and Feisty Master_ A System with Ten Pounds of Rebellion - Kang Jinghan 2 (Patreon) [AI Generated] [Decensored] [113P].zip#02092_3248325916.png"
    imageFilename = "02092_3248325916.png"
    thumbnailWidth = 300
    thumbnailHeight = 300
    userId = $null
    jobId = $null
    id = [Guid]::NewGuid().ToString()
    occurredOn = (Get-Date).ToUniversalTime().ToString("o")
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    messageType = "ThumbnailGeneration"
    correlationId = $null
    properties = @{}
} | ConvertTo-Json -Compress

Write-Host "Message: $message" -ForegroundColor Gray
Write-Host ""

# Publish to RabbitMQ via management API
try {
    $body = @{
        properties = @{}
        routing_key = "thumbnail.generation"
        payload = $message
        payload_encoding = "string"
    } | ConvertTo-Json

    $result = Invoke-RestMethod `
        -Uri "http://localhost:15672/api/exchanges/%2F/imageviewer.exchange/publish" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -Credential (New-Object System.Management.Automation.PSCredential("guest", (ConvertTo-SecureString "guest" -AsPlainText -Force))) `
        -AllowUnencryptedAuthentication
    
    if ($result.routed) {
        Write-Host "✅ Thumbnail message published successfully!" -ForegroundColor Green
        Write-Host "Check worker logs for processing..." -ForegroundColor Yellow
    } else {
        Write-Host "❌ Message not routed" -ForegroundColor Red
    }
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

