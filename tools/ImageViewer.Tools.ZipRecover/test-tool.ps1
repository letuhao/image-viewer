# Test script for ZIP Recovery Tool
# This script demonstrates how to test the tool with sample data

Write-Host "ZIP Recovery Tool Test Script" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Check if the tool is built
if (-not (Test-Path "bin\Debug\net9.0\ImageViewer.Tools.ZipRecover.exe")) {
    Write-Host "Building ZIP Recovery Tool..." -ForegroundColor Yellow
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Check if input file exists
if (-not (Test-Path "data\input.txt")) {
    Write-Host "Input file not found: data\input.txt" -ForegroundColor Red
    Write-Host "Please ensure the input file exists with log entries containing ZIP file paths." -ForegroundColor Yellow
    exit 1
}

# Check if 7-Zip is available
$sevenZipPath = "C:\Program Files\7-Zip\7z.exe"
if (Test-Path $sevenZipPath) {
    Write-Host "✅ 7-Zip found at: $sevenZipPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  7-Zip not found at: $sevenZipPath" -ForegroundColor Yellow
    Write-Host "   Tool will fall back to .NET ZipFile" -ForegroundColor Yellow
}

# Create backup directory if it doesn't exist
if (-not (Test-Path "backup_corrupted")) {
    New-Item -ItemType Directory -Path "backup_corrupted" -Force | Out-Null
    Write-Host "✅ Created backup directory: backup_corrupted" -ForegroundColor Green
}

# Create temp directory if it doesn't exist
if (-not (Test-Path "temp_recovery")) {
    New-Item -ItemType Directory -Path "temp_recovery" -Force | Out-Null
    Write-Host "✅ Created temp directory: temp_recovery" -ForegroundColor Green
}

Write-Host ""
Write-Host "Running ZIP Recovery Tool..." -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Run the tool
dotnet run

Write-Host ""
Write-Host "ZIP Recovery Tool completed!" -ForegroundColor Green
Write-Host "Check the logs above for results." -ForegroundColor Green
Write-Host ""
Write-Host "Files processed:" -ForegroundColor Yellow
Write-Host "- Original corrupted ZIPs: Moved to backup_corrupted\" -ForegroundColor Yellow
Write-Host "- Recovered ZIPs: Replaced original files" -ForegroundColor Yellow
Write-Host "- Temp files: Automatically cleaned up" -ForegroundColor Yellow
