# ========================================
# ImageViewer Platform - Stop All Services
# ========================================
# This script stops all ImageViewer services running in background
#
# Usage:
#   .\stop-all-services.ps1
# ========================================

param(
    [switch]$Force = $false
)

# Colors for output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    $timestamp = Get-Date -Format "HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
}

function Write-Success { param([string]$Message) Write-ColorOutput $Message "Green" }
function Write-Info { param([string]$Message) Write-ColorOutput $Message "Cyan" }
function Write-Warning { param([string]$Message) Write-ColorOutput $Message "Yellow" }
function Write-Error { param([string]$Message) Write-ColorOutput $Message "Red" }

# Header
Clear-Host
Write-Host "========================================" -ForegroundColor Red
Write-Host " ImageViewer Platform - Stop Services" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

Write-Info "Searching for ImageViewer processes..."
Write-Host ""

# Find and stop processes
$processNames = @(
    "ImageViewer.Api",
    "ImageViewer.Worker", 
    "ImageViewer.Scheduler"
)

$stoppedCount = 0
$totalFound = 0

foreach ($processName in $processNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    
    if ($processes) {
        foreach ($proc in $processes) {
            $totalFound++
            try {
                Write-Info "  • Stopping $processName (PID: $($proc.Id))..."
                
                if ($Force) {
                    Stop-Process -Id $proc.Id -Force
                } else {
                    # Try graceful shutdown first
                    $proc.CloseMainWindow() | Out-Null
                    Start-Sleep -Milliseconds 500
                    
                    if (-not $proc.HasExited) {
                        Stop-Process -Id $proc.Id -Force
                    }
                }
                
                Write-Success "    ✓ Stopped $processName"
                $stoppedCount++
            } catch {
                Write-Error "    ✗ Failed to stop $processName (PID: $($proc.Id)): $_"
            }
        }
    }
}

# Stop frontend (node processes in client folder)
Write-Info "  • Stopping Frontend (Node.js)..."
$nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue

foreach ($proc in $nodeProcesses) {
    try {
        $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)" -ErrorAction SilentlyContinue).CommandLine
        
        if ($cmdLine -match "image-viewer\\client" -or $cmdLine -match "vite") {
            $totalFound++
            Write-Info "    • Found Frontend process (PID: $($proc.Id))"
            Stop-Process -Id $proc.Id -Force
            Write-Success "    ✓ Stopped Frontend"
            $stoppedCount++
        }
    } catch {
        # Process might have already exited
    }
}

Write-Host ""

# Summary
if ($totalFound -eq 0) {
    Write-Warning "No ImageViewer processes found running"
} else {
    Write-Success "Stopped $stoppedCount of $totalFound process(es)"
    
    if ($stoppedCount -lt $totalFound) {
        Write-Warning "Some processes could not be stopped. Try running with -Force parameter:"
        Write-Warning "  .\stop-all-services.ps1 -Force"
    }
}

# Clean up hung processes
Write-Host ""
Write-Info "Checking for remaining processes..."
Start-Sleep -Seconds 2

$remaining = @()
foreach ($processName in $processNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        $remaining += $processes
    }
}

if ($remaining.Count -gt 0) {
    Write-Warning "Warning: $($remaining.Count) process(es) still running:"
    foreach ($proc in $remaining) {
        Write-Warning "  • $($proc.ProcessName) (PID: $($proc.Id))"
    }
    Write-Host ""
    Write-Warning "To force kill, run:"
    Write-Warning "  Get-Process ImageViewer* | Stop-Process -Force"
} else {
    Write-Success "✓ All ImageViewer processes stopped cleanly"
}

Write-Host ""
Write-Success "Shutdown complete!"
Write-Host ""

