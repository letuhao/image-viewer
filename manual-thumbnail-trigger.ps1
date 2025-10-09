# Manual Thumbnail Generation Trigger
# This script manually queues thumbnail generation for all images that don't have thumbnails

Write-Host "=== Manual Thumbnail Generation Trigger ===" -ForegroundColor Cyan
Write-Host "This will queue thumbnail generation for all images" -ForegroundColor Yellow
Write-Host ""

# Get collections with images but no thumbnails
Write-Host "Finding collections with images..." -ForegroundColor Gray
$collectionsJson = mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval @"
db.collections.find(
    { 'images.0': { `$exists: true } }, 
    { _id: 1, name: 1 }
).toArray()
"@

# Parse the collections
try {
    $collections = $collectionsJson | ConvertFrom-Json
    Write-Host "Found $($collections.Count) collections with images" -ForegroundColor Green
    Write-Host ""

    foreach ($col in $collections) {
        Write-Host "Processing collection: $($col.name)" -ForegroundColor Cyan
        
        # Trigger collection scan to regenerate thumbnails
        $collectionId = $col._id.'$oid'
        
        # Create scan job via API
        $body = @{
            "collectionId" = $collectionId
            "forceRescan" = $true
        } | ConvertTo-Json
        
        try {
            $result = Invoke-RestMethod `
                -Uri "http://localhost:11000/api/v1/collections/$collectionId/scan" `
                -Method Post `
                -ContentType "application/json" `
                -Body $body `
                -ErrorAction Stop
            
            Write-Host "  ✅ Scan queued for $($col.name)" -ForegroundColor Green
        }
        catch {
            Write-Host "  ❌ Failed to queue scan: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        Start-Sleep -Milliseconds 100
    }
    
    Write-Host ""
    Write-Host "✅ Thumbnail generation triggered for all collections" -ForegroundColor Green
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
}

