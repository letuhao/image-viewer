# WPF ZIP Recovery Tool Launcher
# This script launches the WPF ZIP Recovery Tool

Write-Host "ZIP Recovery Tool - WPF Application" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green

# Check if .NET 9.0 is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    Write-Host "   Download from: https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Yellow
    exit 1
}

# Check if the project exists
if (-not (Test-Path "ImageViewer.Tools.ZipRecover.csproj")) {
    Write-Host "❌ Project file not found. Please run this script from the project directory." -ForegroundColor Red
    exit 1
}

# Check if input file exists
if (-not (Test-Path "data\input.txt")) {
    Write-Host "⚠️  Input file not found: data\input.txt" -ForegroundColor Yellow
    Write-Host "   The application will show an error when trying to load files." -ForegroundColor Yellow
    Write-Host "   Please ensure the input file exists with log entries containing ZIP file paths." -ForegroundColor Yellow
}

# Check if 7-Zip is available
$sevenZipPath = "C:\Program Files\7-Zip\7z.exe"
if (Test-Path $sevenZipPath) {
    Write-Host "✅ 7-Zip found at: $sevenZipPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  7-Zip not found at: $sevenZipPath" -ForegroundColor Yellow
    Write-Host "   Application will fall back to .NET ZipFile" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Building and launching WPF ZIP Recovery Tool..." -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

# Build the application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build --configuration Release

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful!" -ForegroundColor Green
Write-Host ""
Write-Host "Launching WPF application..." -ForegroundColor Cyan

# Launch the WPF application
dotnet run --configuration Release

Write-Host ""
Write-Host "ZIP Recovery Tool has been closed." -ForegroundColor Green
Write-Host "Check the application logs and results above." -ForegroundColor Green
