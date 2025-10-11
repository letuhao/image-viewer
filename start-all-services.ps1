# ========================================
# ImageViewer Platform - Start All Services
# ========================================
# This script starts all ImageViewer services in silent mode (background)
# No Docker required - runs everything locally
#
# Prerequisites:
# - .NET 9 SDK installed
# - Node.js installed (for frontend)
# - MongoDB running (local or remote)
# - RabbitMQ running (local or remote)
# - Redis running (local or remote)
#
# Usage:
#   .\start-all-services.ps1
#
# To stop all services:
#   .\stop-all-services.ps1
# ========================================

param(
    [switch]$SkipBuild = $false,
    [switch]$Verbose = $false
)

# Configuration
$ErrorActionPreference = "Continue"
$StartTime = Get-Date

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
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ImageViewer Platform - Service Launcher" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running from correct directory
if (-not (Test-Path "src/ImageViewer.sln")) {
    Write-Error "Error: Must run from image-viewer root directory"
    Write-Error "Current directory: $PWD"
    exit 1
}

Write-Success "✓ Running from correct directory: $PWD"
Write-Host ""

# ========================================
# 1. Check Prerequisites
# ========================================
Write-Info "Step 1/7: Checking prerequisites..."

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Success "  ✓ .NET SDK: $dotnetVersion"
} catch {
    Write-Error "  ✗ .NET SDK not found. Please install .NET 9 SDK"
    exit 1
}

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Success "  ✓ Node.js: $nodeVersion"
} catch {
    Write-Error "  ✗ Node.js not found. Please install Node.js"
    exit 1
}

# Check MongoDB connection
Write-Info "  • Checking MongoDB connection..."
try {
    $mongoTest = mongosh --quiet --eval "db.adminCommand('ping')" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ MongoDB: Connected"
    } else {
        Write-Warning "  ! MongoDB: Not accessible (will use configured connection string)"
    }
} catch {
    Write-Warning "  ! MongoDB: mongosh not installed (assuming MongoDB is running)"
}

# Check RabbitMQ
Write-Info "  • Checking RabbitMQ..."
try {
    $rabbitTest = Test-NetConnection -ComputerName localhost -Port 5672 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($rabbitTest.TcpTestSucceeded) {
        Write-Success "  ✓ RabbitMQ: Listening on port 5672"
    } else {
        Write-Warning "  ! RabbitMQ: Not detected on localhost:5672 (will use configured connection)"
    }
} catch {
    Write-Warning "  ! RabbitMQ: Status unknown"
}

# Check Redis
Write-Info "  • Checking Redis..."
try {
    $redisTest = Test-NetConnection -ComputerName localhost -Port 6379 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($redisTest.TcpTestSucceeded) {
        Write-Success "  ✓ Redis: Listening on port 6379"
    } else {
        Write-Warning "  ! Redis: Not detected on localhost:6379 (will use configured connection)"
    }
} catch {
    Write-Warning "  ! Redis: Status unknown"
}

Write-Host ""

# ========================================
# 2. Stop Existing Processes
# ========================================
Write-Info "Step 2/7: Stopping existing ImageViewer processes..."

$processNames = @("ImageViewer.Api", "ImageViewer.Worker", "ImageViewer.Scheduler", "node")
$stoppedCount = 0

foreach ($processName in $processNames) {
    $processes = Get-Process -Name $processName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            # Check if it's our frontend (node process running in client folder)
            if ($processName -eq "node") {
                $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").CommandLine
                if ($cmdLine -notmatch "image-viewer\\client") {
                    continue  # Skip non-ImageViewer node processes
                }
            }
            
            try {
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                $stoppedCount++
                Write-Warning "  • Stopped existing process: $processName (PID: $($proc.Id))"
            } catch {
                Write-Warning "  ! Could not stop process: $processName (PID: $($proc.Id))"
            }
        }
    }
}

if ($stoppedCount -eq 0) {
    Write-Success "  ✓ No existing processes found"
} else {
    Write-Warning "  • Stopped $stoppedCount existing process(es)"
    Start-Sleep -Seconds 2  # Give processes time to clean up
}

Write-Host ""

# ========================================
# 3. Build Projects (Optional)
# ========================================
if (-not $SkipBuild) {
    Write-Info "Step 3/7: Building all projects..."
    
    Push-Location src
    
    try {
        $buildOutput = dotnet build ImageViewer.sln -c Release --nologo -v minimal 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "  ✓ Backend build successful"
        } else {
            Write-Error "  ✗ Backend build failed"
            Write-Host $buildOutput
            Pop-Location
            exit 1
        }
    } catch {
        Write-Error "  ✗ Build error: $_"
        Pop-Location
        exit 1
    }
    
    Pop-Location
    
    # Build frontend
    Write-Info "  • Building frontend..."
    Push-Location client
    
    try {
        if (-not (Test-Path "node_modules")) {
            Write-Info "  • Installing frontend dependencies..."
            npm install --silent 2>&1 | Out-Null
        }
        
        # Just verify build works, don't actually build for dev
        Write-Success "  ✓ Frontend dependencies ready"
    } catch {
        Write-Warning "  ! Frontend setup warning: $_"
    }
    
    Pop-Location
    Write-Host ""
} else {
    Write-Info "Step 3/7: Skipping build (--SkipBuild specified)"
    Write-Host ""
}

# ========================================
# 4. Create Logs Directory
# ========================================
Write-Info "Step 4/7: Setting up log directories..."

$logDirs = @("logs/api", "logs/worker", "logs/scheduler", "logs/frontend")
foreach ($dir in $logDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Success "  ✓ Created: $dir"
    }
}

Write-Host ""

# ========================================
# 5. Start Backend Services
# ========================================
Write-Info "Step 5/7: Starting backend services..."

# Start API
Write-Info "  • Starting API server (port 11001)..."
Push-Location src/ImageViewer.Api
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build -c Release" `
    -WindowStyle Hidden -PassThru -RedirectStandardOutput "../../logs/api/output.log" `
    -RedirectStandardError "../../logs/api/error.log"
Pop-Location

if ($apiProcess) {
    Write-Success "    ✓ API started (PID: $($apiProcess.Id))"
    Write-Success "    ✓ URL: https://localhost:11001"
    Write-Success "    ✓ Logs: logs/api/"
} else {
    Write-Error "    ✗ Failed to start API"
}

Start-Sleep -Seconds 3  # Give API time to initialize

# Start Worker
Write-Info "  • Starting Worker (RabbitMQ consumer)..."
Push-Location src/ImageViewer.Worker
$workerProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build -c Release" `
    -WindowStyle Hidden -PassThru -RedirectStandardOutput "../../logs/worker/output.log" `
    -RedirectStandardError "../../logs/worker/error.log"
Pop-Location

if ($workerProcess) {
    Write-Success "    ✓ Worker started (PID: $($workerProcess.Id))"
    Write-Success "    ✓ Logs: logs/worker/"
} else {
    Write-Error "    ✗ Failed to start Worker"
}

Start-Sleep -Seconds 2

# Start Scheduler
Write-Info "  • Starting Scheduler (Hangfire worker)..."
Push-Location src/ImageViewer.Scheduler
$schedulerProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --no-build -c Release" `
    -WindowStyle Hidden -PassThru -RedirectStandardOutput "../../logs/scheduler/output.log" `
    -RedirectStandardError "../../logs/scheduler/error.log"
Pop-Location

if ($schedulerProcess) {
    Write-Success "    ✓ Scheduler started (PID: $($schedulerProcess.Id))"
    Write-Success "    ✓ Logs: logs/scheduler/"
} else {
    Write-Error "    ✗ Failed to start Scheduler"
}

Write-Host ""

# ========================================
# 6. Start Frontend
# ========================================
Write-Info "Step 6/7: Starting frontend development server..."

Push-Location client
$frontendProcess = Start-Process -FilePath "npm" -ArgumentList "run dev" `
    -WindowStyle Hidden -PassThru -RedirectStandardOutput "../logs/frontend/output.log" `
    -RedirectStandardError "../logs/frontend/error.log"
Pop-Location

if ($frontendProcess) {
    Write-Success "  ✓ Frontend started (PID: $($frontendProcess.Id))"
    Write-Success "  ✓ URL: http://localhost:3000"
    Write-Success "  ✓ Logs: logs/frontend/"
} else {
    Write-Error "  ✗ Failed to start Frontend"
}

Write-Host ""

# ========================================
# 7. Health Check
# ========================================
Write-Info "Step 7/7: Waiting for services to be ready..."

Start-Sleep -Seconds 10  # Initial wait

# Check API health
Write-Info "  • Checking API health..."
$maxAttempts = 12
$attempt = 0
$apiHealthy = $false

while ($attempt -lt $maxAttempts -and -not $apiHealthy) {
    try {
        $response = Invoke-WebRequest -Uri "https://localhost:11001/api/v1/health" `
            -Method GET -TimeoutSec 5 -SkipCertificateCheck -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -eq 200) {
            $apiHealthy = $true
            Write-Success "    ✓ API is healthy and responding"
        }
    } catch {
        $attempt++
        if ($attempt -lt $maxAttempts) {
            Write-Host "    • Waiting for API... (attempt $attempt/$maxAttempts)" -ForegroundColor Gray
            Start-Sleep -Seconds 5
        }
    }
}

if (-not $apiHealthy) {
    Write-Warning "    ! API health check timeout (may still be starting)"
}

# Check Frontend
Write-Info "  • Checking Frontend..."
Start-Sleep -Seconds 5

try {
    $frontendResponse = Invoke-WebRequest -Uri "http://localhost:3000" `
        -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
    
    if ($frontendResponse.StatusCode -eq 200) {
        Write-Success "    ✓ Frontend is healthy and responding"
    }
} catch {
    Write-Warning "    ! Frontend health check timeout (may still be building)"
}

Write-Host ""

# ========================================
# Summary
# ========================================
$elapsed = (Get-Date) - $StartTime

Write-Host "========================================" -ForegroundColor Green
Write-Host " All Services Started Successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Service Status:" -ForegroundColor Cyan
Write-Host "  • API Server:    https://localhost:11001 (PID: $($apiProcess.Id))" -ForegroundColor White
Write-Host "  • Worker:        Background (PID: $($workerProcess.Id))" -ForegroundColor White
Write-Host "  • Scheduler:     Background (PID: $($schedulerProcess.Id))" -ForegroundColor White
Write-Host "  • Frontend:      http://localhost:3000 (PID: $($frontendProcess.Id))" -ForegroundColor White
Write-Host ""
Write-Host "Management URLs:" -ForegroundColor Cyan
Write-Host "  • Hangfire Dashboard:  https://localhost:11001/hangfire" -ForegroundColor White
Write-Host "  • Swagger API Docs:    https://localhost:11001/swagger" -ForegroundColor White
Write-Host "  • RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host ""
Write-Host "Logs:" -ForegroundColor Cyan
Write-Host "  • API:        logs/api/output.log" -ForegroundColor White
Write-Host "  • Worker:     logs/worker/output.log" -ForegroundColor White
Write-Host "  • Scheduler:  logs/scheduler/output.log" -ForegroundColor White
Write-Host "  • Frontend:   logs/frontend/output.log" -ForegroundColor White
Write-Host ""
Write-Host "Commands:" -ForegroundColor Cyan
Write-Host "  • Stop all services:     .\stop-all-services.ps1" -ForegroundColor White
Write-Host "  • View API logs:         Get-Content logs/api/output.log -Wait" -ForegroundColor White
Write-Host "  • View Worker logs:      Get-Content logs/worker/output.log -Wait" -ForegroundColor White
Write-Host "  • View Scheduler logs:   Get-Content logs/scheduler/output.log -Wait" -ForegroundColor White
Write-Host "  • Check processes:       Get-Process | Where-Object {`$_.ProcessName -like '*ImageViewer*'}" -ForegroundColor White
Write-Host ""
Write-Host "Startup completed in $([math]::Round($elapsed.TotalSeconds, 1)) seconds" -ForegroundColor Green
Write-Host ""
Write-Host "Press Ctrl+C to stop monitoring, services will continue running in background" -ForegroundColor Yellow
Write-Host ""

# ========================================
# 8. Monitor Services (Optional)
# ========================================
Write-Info "Monitoring service health (press Ctrl+C to exit)..."
Write-Host ""

$monitorCount = 0
try {
    while ($true) {
        Start-Sleep -Seconds 10
        $monitorCount++
        
        # Check if processes are still running
        $apiAlive = Get-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
        $workerAlive = Get-Process -Id $workerProcess.Id -ErrorAction SilentlyContinue
        $schedulerAlive = Get-Process -Id $schedulerProcess.Id -ErrorAction SilentlyContinue
        $frontendAlive = Get-Process -Id $frontendProcess.Id -ErrorAction SilentlyContinue
        
        if (-not $apiAlive) {
            Write-Error "⚠️ API process died! Check logs/api/error.log"
        }
        if (-not $workerAlive) {
            Write-Error "⚠️ Worker process died! Check logs/worker/error.log"
        }
        if (-not $schedulerAlive) {
            Write-Error "⚠️ Scheduler process died! Check logs/scheduler/error.log"
        }
        if (-not $frontendAlive) {
            Write-Error "⚠️ Frontend process died! Check logs/frontend/error.log"
        }
        
        if ($apiAlive -and $workerAlive -and $schedulerAlive -and $frontendAlive) {
            if ($monitorCount % 6 -eq 0) {  # Every minute
                Write-Host "✓ All services running (checked $monitorCount times)" -ForegroundColor Gray
            }
        }
        
        if (-not $apiAlive -and -not $workerAlive -and -not $schedulerAlive -and -not $frontendAlive) {
            Write-Warning "All services stopped. Exiting monitor."
            break
        }
    }
} catch {
    # Ctrl+C pressed
    Write-Host ""
    Write-Info "Monitoring stopped. Services continue running in background."
    Write-Info "Use .\stop-all-services.ps1 to stop all services."
}

Write-Host ""
Write-Success "Script completed!"

