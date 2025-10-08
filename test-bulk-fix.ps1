# Test bulk collections with fixed logic
$body = @{
    ParentPath = "L:\EMedia\AI_Generated\AiASAG"
    CollectionPrefix = ""
    IncludeSubfolders = $true
    AutoAdd = $true
    ThumbnailWidth = 300
    ThumbnailHeight = 300
    CacheWidth = 1920
    CacheHeight = 1080
    EnableCache = $true
    AutoScan = $true
} | ConvertTo-Json

Write-Host "Testing bulk collections with fixed logic..." -ForegroundColor Green
Write-Host "Request body: $body" -ForegroundColor Cyan

try {
    $response = Invoke-RestMethod -Uri "http://localhost:11000/api/v1/bulk/collections" -Method POST -ContentType "application/json" -Body $body
    Write-Host "Response:" -ForegroundColor Green
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}
