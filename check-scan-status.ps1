#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Quick check of library scan status
#>

param(
    [string]$LibraryId = "68ea89ea433a1e43a6917afa"
)

Write-Host "`n=== Library Scan Status Check ===" -ForegroundColor Cyan
Write-Host "Library ID: $LibraryId`n" -ForegroundColor Gray

# Check if path exists and has content
Write-Host "[1] Checking Library Path..." -ForegroundColor Yellow
mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval "db.libraries.findOne({_id: ObjectId('$LibraryId')}, {name: 1, path: 1})" | Out-String | Write-Host

$libraryPath = (mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval "db.libraries.findOne({_id: ObjectId('$LibraryId')}).path" 2>$null) -replace '"', ''

if ($libraryPath -and (Test-Path $libraryPath)) {
    Write-Host "✅ Path exists: $libraryPath" -ForegroundColor Green
    
    $folders = Get-ChildItem $libraryPath -Directory -ErrorAction SilentlyContinue
    Write-Host "  Folders found: $($folders.Count)" -ForegroundColor Cyan
    
    if ($folders.Count -eq 0) {
        Write-Host "  ⚠️ No subfolders found - Worker will find 0 collections!" -ForegroundColor Yellow
        Write-Host "  💡 TIP: Add folders with images to $libraryPath" -ForegroundColor Gray
    } else {
        Write-Host "  Folders:" -ForegroundColor Cyan
        $folders | Select-Object -First 5 | ForEach-Object {
            $imageCount = (Get-ChildItem $_.FullName -File -Include *.jpg,*.jpeg,*.png,*.gif -ErrorAction SilentlyContinue).Count
            Write-Host "    - $($_.Name) ($imageCount images)" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "❌ Path does not exist!" -ForegroundColor Red
}

Write-Host "`n[2] Checking Collections Created..." -ForegroundColor Yellow
$collections = mongosh "mongodb://localhost:27017/image_viewer" --quiet --eval "db.collections.find({libraryId: ObjectId('$LibraryId')}).toArray()"

if ($collections -eq "[]") {
    Write-Host "  ⚠️  No collections created yet" -ForegroundColor Yellow
} else {
    Write-Host "✅ Collections found:" -ForegroundColor Green
    Write-Host $collections
}

Write-Host "`n[3] Checking Worker Logs..." -ForegroundColor Yellow
if (Test-Path "src/ImageViewer.Worker/logs") {
    $latestLog = Get-ChildItem "src/ImageViewer.Worker/logs" -Filter "worker-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latestLog) {
        Write-Host "  Last 10 lines from Worker log:" -ForegroundColor Cyan
        Get-Content $latestLog.FullName -Tail 10 | ForEach-Object {
            if ($_ -match "library|collection|scan") {
                Write-Host "  $_" -ForegroundColor White
            }
        }
    }
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "If no collections created, check:" -ForegroundColor Yellow
Write-Host "  1. Does L:\test have subfolders?" -ForegroundColor Gray
Write-Host "  2. Do those subfolders contain .jpg/.png files?" -ForegroundColor Gray
Write-Host "  3. Is Worker running? (.\status-services.ps1)" -ForegroundColor Gray
Write-Host "  4. Check Worker logs for errors" -ForegroundColor Gray
Write-Host ""

