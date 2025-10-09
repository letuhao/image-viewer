# Queue Thumbnail Generation for All Images
# This script queues thumbnail generation for all 3,198 images

Write-Host "=== Queuing Thumbnails for All Images ===" -ForegroundColor Cyan
Write-Host "This will create ~3,198 thumbnail generation messages" -ForegroundColor Yellow
Write-Host ""

# Get all collections and their images
Write-Host "Fetching collections and images from MongoDB..." -ForegroundColor Gray

$script = @'
var collections = db.collections.find({'images.0': {$exists: true}}, {_id: 1, name: 1, path: 1, images: 1}).toArray();
var messages = [];
collections.forEach(col => {
    if (col.images) {
        col.images.forEach(img => {
            messages.push({
                imageId: img._id.toString(),
                collectionId: col._id.toString(),
                imagePath: col.path + '#' + img.relativePath,
                filename: img.filename
            });
        });
    }
});
print(JSON.stringify(messages));
'@

$imagesJson = mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval $script
$images = $imagesJson | ConvertFrom-Json

Write-Host "Found $($images.Count) images to process" -ForegroundColor Green
Write-Host ""

# RabbitMQ credentials
$cred = New-Object System.Management.Automation.PSCredential("guest", (ConvertTo-SecureString "guest" -AsPlainText -Force))

# Queue messages in batches
$batchSize = 100
$totalQueued = 0
$errors = 0

for ($i = 0; $i -lt $images.Count; $i += $batchSize) {
    $batch = $images[$i..([Math]::Min($i + $batchSize - 1, $images.Count - 1))]
    
    foreach ($img in $batch) {
        $message = @{
            imageId = $img.imageId
            collectionId = $img.collectionId
            imagePath = $img.imagePath
            imageFilename = [System.IO.Path]::GetFileName($img.filename)
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
                -Credential $cred `
                -AllowUnencryptedAuthentication `
                -ErrorAction Stop
            
            if ($result.routed) {
                $totalQueued++
            }
        }
        catch {
            $errors++
            Write-Host "Error queuing thumbnail for $($img.filename): $_" -ForegroundColor Red
        }
    }
    
    Write-Host "Progress: $totalQueued / $($images.Count) queued..." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Total Queued: $totalQueued" -ForegroundColor Green
Write-Host "Errors: $errors" -ForegroundColor $(if ($errors -gt 0) {'Red'} else {'Green'})
Write-Host ""
Write-Host "Monitor progress with:" -ForegroundColor Yellow
Write-Host "  Get-Content 'src\ImageViewer.Worker\logs\imageviewer-worker20251009.log' -Tail 50 -Wait" -ForegroundColor Gray

