# Quick test to see what GetByIdAsync returns for nested collections
Write-Host "`n🧪 Testing MongoDB Deserialization`n" -ForegroundColor Cyan

$testIds = @(
    @{Name="Working: Geldoru 92P"; Id="68e8168e72306e799da83671"},
    @{Name="NOT Working: Daikon Wedding"; Id="68e8168e72306e799da8367b"},
    @{Name="NOT Working: Daikon 2B"; Id="68e8168e72306e799da8367f"}
)

foreach ($test in $testIds) {
    Write-Host "$($test.Name):" -ForegroundColor Yellow
    Write-Host "  ID: $($test.Id)" -ForegroundColor Gray
    
    # Query MongoDB directly
    $mongoResult = mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval "
var coll = db.collections.findOne({_id: ObjectId('$($test.Id)')});
print(coll.images.length + '/' + coll.thumbnails.length + '/' + coll.cacheImages.length);
" 2>$null
    
    Write-Host "  MongoDB direct: $($mongoResult.Trim())" -ForegroundColor Green
    Write-Host ""
}

Write-Host "`n💡 The issue: C# driver returns 0 for arrays even though MongoDB has data!" -ForegroundColor Yellow
Write-Host "💡 Possible causes:" -ForegroundColor Yellow
Write-Host "  1. Property setters are 'private set' - driver can't set them" -ForegroundColor Gray
Write-Host "  2. BsonIgnoreIfDefault or similar attribute issue" -ForegroundColor Gray
Write-Host "  3. Driver version mismatch" -ForegroundColor Gray
Write-Host "  4. Arrays need [BsonRequired] or similar" -ForegroundColor Gray
Write-Host ""

