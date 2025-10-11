# ========================================
# ImageViewer Platform - Start All Services (SAFE MODE)
# ========================================
# ANTIVIRUS-FRIENDLY VERSION
# 
# This version shows visible console windows to avoid antivirus false positives.
# Less convenient but works on systems with strict antivirus policies.
#
# Differences from standard version:
# - Shows visible console windows (not hidden)
# - Uses Start-Job instead of Start-Process for some tasks
# - Avoids process enumeration
# - No forced process termination
#
# Usage:
#   .\start-all-services-safe.ps1
#
# Note: You'll see 4 console windows (API, Worker, Scheduler, Frontend)
# ========================================

param(
    [switch]$SkipBuild = $false
)

$ErrorActionPreference = "Continue"
$StartTime = Get-Date

# Colors
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

function Write-Success { param([string]$Message) Write-ColorOutput $Message "Green" }
function Write-Info { param([string]$Message) Write-ColorOutput $Message "Cyan" }
function Write-Warning { param([string]$Message) Write-ColorOutput $Message "Yellow" }
function Write-Error { param([string]$Message) Write-ColorOutput $Message "Red" }

# Header
Clear-Host
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ImageViewer - Start Services (SAFE)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "ANTIVIRUS-FRIENDLY MODE: Console windows will be VISIBLE" -ForegroundColor Yellow
Write-Host ""

# Check directory
if (-not (Test-Path "src/ImageViewer.sln")) {
    Write-Error "Error: Must run from image-viewer root directory"
    exit 1
}

Write-Success "✓ Running from: $PWD"
Write-Host ""

# ========================================
# Build Projects (Optional)
# ========================================
if (-not $SkipBuild) {
    Write-Info "Building all projects..."
    
    Push-Location src
    
    try {
        dotnet build ImageViewer.sln -c Release --nologo -v minimal
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "  ✓ Backend build successful"
        } else {
            Write-Error "  ✗ Backend build failed"
            Pop-Location
            exit 1
        }
    } catch {
        Write-Error "  ✗ Build error: $_"
        Pop-Location
        exit 1
    }
    
    Pop-Location
    Write-Host ""
}

# ========================================
# Create Logs Directory
# ========================================
Write-Info "Setting up log directories..."
$logDirs = @("logs/api", "logs/worker", "logs/scheduler", "logs/frontend")
foreach ($dir in $logDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}
Write-Host ""

# ========================================
# Start Services with VISIBLE Windows
# ========================================
Write-Info "Starting services (visible console windows)..."
Write-Host ""

# Start API (use cmd to keep window open)
Write-Info "  [1/4] Starting API Server..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\ImageViewer.Api'; dotnet run -c Release" `
    -WindowStyle Normal

Start-Sleep -Seconds 5

# Start Worker
Write-Info "  [2/4] Starting Worker..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\ImageViewer.Worker'; dotnet run -c Release" `
    -WindowStyle Normal

Start-Sleep -Seconds 3

# Start Scheduler
Write-Info "  [3/4] Starting Scheduler..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\src\ImageViewer.Scheduler'; dotnet run -c Release" `
    -WindowStyle Normal

Start-Sleep -Seconds 3

# Start Frontend
Write-Info "  [4/4] Starting Frontend..."
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$PWD\client'; npm run dev" `
    -WindowStyle Normal

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " All Services Started!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "You should now see 4 console windows:" -ForegroundColor Cyan
Write-Host "  1. API Server (ImageViewer.Api)" -ForegroundColor White
Write-Host "  2. Worker (ImageViewer.Worker)" -ForegroundColor White
Write-Host "  3. Scheduler (ImageViewer.Scheduler)" -ForegroundColor White
Write-Host "  4. Frontend (npm run dev)" -ForegroundColor White
Write-Host ""
Write-Host "Access URLs:" -ForegroundColor Cyan
Write-Host "  • Frontend:  http://localhost:3000" -ForegroundColor White
Write-Host "  • API:       https://localhost:11001" -ForegroundColor White
Write-Host "  • Swagger:   https://localhost:11001/swagger" -ForegroundColor White
Write-Host "  • Hangfire:  https://localhost:11001/hangfire" -ForegroundColor White
Write-Host ""
Write-Host "To stop services:" -ForegroundColor Yellow
Write-Host "  Close each console window manually, or run:" -ForegroundColor Yellow
Write-Host "  .\stop-all-services.ps1" -ForegroundColor Yellow
Write-Host ""
Write-Success "Startup completed in $([math]::Round(((Get-Date) - $StartTime).TotalSeconds, 1)) seconds"

