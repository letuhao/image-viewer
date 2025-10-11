# ========================================
# ImageViewer Platform - Log Viewer
# ========================================
# View logs from all services in a convenient way
#
# Usage:
#   .\view-logs.ps1                    # Interactive menu
#   .\view-logs.ps1 -Service api       # View specific service
#   .\view-logs.ps1 -Service api -Follow  # Follow/tail logs
#   .\view-logs.ps1 -All               # View all logs combined
# ========================================

param(
    [string]$Service = "",
    [switch]$Follow = $false,
    [switch]$All = $false,
    [int]$Lines = 50
)

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

# Header
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ImageViewer Platform - Log Viewer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$logFiles = @{
    "api" = @{
        Path = "logs/api/output.log"
        Error = "logs/api/error.log"
        Name = "API Server"
        Color = "Cyan"
    }
    "worker" = @{
        Path = "logs/worker/output.log"
        Error = "logs/worker/error.log"
        Name = "Worker"
        Color = "Green"
    }
    "scheduler" = @{
        Path = "logs/scheduler/output.log"
        Error = "logs/scheduler/error.log"
        Name = "Scheduler"
        Color = "Magenta"
    }
    "frontend" = @{
        Path = "logs/frontend/output.log"
        Error = "logs/frontend/error.log"
        Name = "Frontend"
        Color = "Yellow"
    }
}

# View all logs combined
if ($All) {
    Write-Host "Viewing combined logs (Ctrl+C to exit)..." -ForegroundColor Cyan
    Write-Host ""
    
    $allLogs = @()
    foreach ($key in $logFiles.Keys) {
        $log = $logFiles[$key]
        if (Test-Path $log.Path) {
            $content = Get-Content $log.Path -Tail $Lines | ForEach-Object { 
                "[$($log.Name)] $_" 
            }
            $allLogs += $content
        }
    }
    
    $allLogs | Sort-Object | ForEach-Object { Write-Host $_ }
    exit
}

# Interactive menu
if ($Service -eq "") {
    Write-Host "Select a service to view logs:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. API Server" -ForegroundColor Cyan
    Write-Host "  2. Worker" -ForegroundColor Green
    Write-Host "  3. Scheduler" -ForegroundColor Magenta
    Write-Host "  4. Frontend" -ForegroundColor Yellow
    Write-Host "  5. All (combined)" -ForegroundColor White
    Write-Host ""
    Write-Host "  0. Exit" -ForegroundColor Gray
    Write-Host ""
    
    $choice = Read-Host "Enter choice (1-5)"
    
    switch ($choice) {
        "1" { $Service = "api" }
        "2" { $Service = "worker" }
        "3" { $Service = "scheduler" }
        "4" { $Service = "frontend" }
        "5" { $All = $true }
        "0" { exit }
        default { 
            Write-Error "Invalid choice"
            exit 1
        }
    }
    
    if (-not $All) {
        $followChoice = Read-Host "Follow logs in real-time? (y/n)"
        if ($followChoice -eq "y" -or $followChoice -eq "Y") {
            $Follow = $true
        }
    }
    
    Write-Host ""
}

# View specific service logs
if (-not $All -and $Service -ne "") {
    $log = $logFiles[$Service.ToLower()]
    
    if (-not $log) {
        Write-Error "Unknown service: $Service"
        Write-Host "Available services: api, worker, scheduler, frontend"
        exit 1
    }
    
    Write-Host "========================================" -ForegroundColor $log.Color
    Write-Host " $($log.Name) Logs" -ForegroundColor $log.Color
    Write-Host "========================================" -ForegroundColor $log.Color
    Write-Host ""
    
    # Check if log file exists
    if (-not (Test-Path $log.Path)) {
        Write-Warning "Log file not found: $($log.Path)"
        Write-Warning "Service may not have been started yet."
        exit 1
    }
    
    if ($Follow) {
        Write-Info "Following logs (Ctrl+C to exit)..."
        Write-Host ""
        Get-Content $log.Path -Wait -Tail $Lines
    } else {
        Write-Info "Showing last $Lines lines..."
        Write-Host ""
        Get-Content $log.Path -Tail $Lines
        
        Write-Host ""
        Write-Host "To follow logs in real-time, run:" -ForegroundColor Gray
        Write-Host "  Get-Content $($log.Path) -Wait" -ForegroundColor Gray
    }
}

# View all combined
if ($All) {
    $allLogs = @()
    foreach ($key in $logFiles.Keys) {
        $log = $logFiles[$key]
        if (Test-Path $log.Path) {
            $content = Get-Content $log.Path -Tail $Lines | ForEach-Object { 
                "[$($log.Name)] $_" 
            }
            $allLogs += $content
        }
    }
    
    Write-Info "Combined logs from all services:"
    Write-Host ""
    $allLogs | ForEach-Object { Write-Host $_ }
}

Write-Host ""

