# ========================================
# ImageViewer Platform - Quick Start
# ========================================
# First-time setup script for ImageViewer platform
# Installs dependencies, builds projects, and starts all services
#
# Usage:
#   .\quick-start.ps1
# ========================================

param(
    [switch]$SkipDependencyCheck = $false
)

$ErrorActionPreference = "Continue"

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
function Write-Step { param([string]$Message) Write-Host ""; Write-Host $Message -ForegroundColor Magenta; Write-Host "" }

# Header
Clear-Host
Write-Host "========================================" -ForegroundColor Magenta
Write-Host " ImageViewer Platform - Quick Start" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "This script will:" -ForegroundColor White
Write-Host "  1. Check prerequisites (.NET, Node.js, MongoDB, RabbitMQ, Redis)" -ForegroundColor White
Write-Host "  2. Restore NuGet packages" -ForegroundColor White
Write-Host "  3. Install frontend dependencies" -ForegroundColor White
Write-Host "  4. Build all projects" -ForegroundColor White
Write-Host "  5. Start all services" -ForegroundColor White
Write-Host ""
Write-Host "Press Ctrl+C to cancel, or any key to continue..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Write-Host ""

# ========================================
# Step 1: Prerequisites
# ========================================
Write-Step "Step 1/5: Checking Prerequisites"

$allPrereqsMet = $true

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -match "^9\.") {
        Write-Success "  ✓ .NET SDK 9.x: $dotnetVersion"
    } else {
        Write-Warning "  ! .NET SDK version: $dotnetVersion (9.x recommended)"
    }
} catch {
    Write-Error "  ✗ .NET SDK not found"
    Write-Error "    Install from: https://dotnet.microsoft.com/download/dotnet/9.0"
    $allPrereqsMet = $false
}

# Check Node.js
try {
    $nodeVersion = node --version
    $npmVersion = npm --version
    Write-Success "  ✓ Node.js: $nodeVersion"
    Write-Success "  ✓ npm: $npmVersion"
} catch {
    Write-Error "  ✗ Node.js not found"
    Write-Error "    Install from: https://nodejs.org/"
    $allPrereqsMet = $false
}

# Check MongoDB
Write-Info "  • Checking MongoDB..."
$mongoTest = Test-NetConnection -ComputerName localhost -Port 27017 -WarningAction SilentlyContinue -InformationLevel Quiet
if ($mongoTest.TcpTestSucceeded) {
    Write-Success "  ✓ MongoDB: Running on localhost:27017"
} else {
    Write-Warning "  ! MongoDB: Not detected on localhost:27017"
    Write-Warning "    Make sure MongoDB is running or update connection string in appsettings.json"
}

# Check RabbitMQ
Write-Info "  • Checking RabbitMQ..."
$rabbitTest = Test-NetConnection -ComputerName localhost -Port 5672 -WarningAction SilentlyContinue -InformationLevel Quiet
if ($rabbitTest.TcpTestSucceeded) {
    Write-Success "  ✓ RabbitMQ: Running on localhost:5672"
} else {
    Write-Warning "  ! RabbitMQ: Not detected on localhost:5672"
    Write-Warning "    Install: https://www.rabbitmq.com/download.html"
    Write-Warning "    Or run via Docker: docker run -d -p 5672:5672 -p 15672:15672 rabbitmq:3-management"
}

# Check Redis
Write-Info "  • Checking Redis..."
$redisTest = Test-NetConnection -ComputerName localhost -Port 6379 -WarningAction SilentlyContinue -InformationLevel Quiet
if ($redisTest.TcpTestSucceeded) {
    Write-Success "  ✓ Redis: Running on localhost:6379"
} else {
    Write-Warning "  ! Redis: Not detected on localhost:6379"
    Write-Warning "    Install or run via Docker: docker run -d -p 6379:6379 redis:alpine"
}

if (-not $allPrereqsMet) {
    Write-Host ""
    Write-Error "Critical prerequisites missing. Please install required software and try again."
    exit 1
}

# ========================================
# Step 2: Restore NuGet Packages
# ========================================
Write-Step "Step 2/5: Restoring NuGet Packages"

Push-Location src

try {
    Write-Info "  • Restoring packages..."
    $restoreOutput = dotnet restore ImageViewer.sln --nologo 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ NuGet packages restored successfully"
    } else {
        Write-Error "  ✗ Package restore failed"
        Write-Host $restoreOutput
        Pop-Location
        exit 1
    }
} catch {
    Write-Error "  ✗ Restore error: $_"
    Pop-Location
    exit 1
}

Pop-Location

# ========================================
# Step 3: Install Frontend Dependencies
# ========================================
Write-Step "Step 3/5: Installing Frontend Dependencies"

Push-Location client

try {
    if (Test-Path "node_modules") {
        Write-Info "  • node_modules exists, checking for updates..."
        npm install --silent
    } else {
        Write-Info "  • Installing dependencies (this may take a few minutes)..."
        npm install
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "  ✓ Frontend dependencies installed"
    } else {
        Write-Error "  ✗ npm install failed"
        Pop-Location
        exit 1
    }
} catch {
    Write-Error "  ✗ Installation error: $_"
    Pop-Location
    exit 1
}

Pop-Location

# ========================================
# Step 4: Build All Projects
# ========================================
Write-Step "Step 4/5: Building All Projects"

Push-Location src

try {
    Write-Info "  • Building backend projects..."
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

# ========================================
# Step 5: Start All Services
# ========================================
Write-Step "Step 5/5: Starting All Services"

Write-Info "  • Launching services in background..."
Write-Host ""

# Call the start-all-services script
& .\start-all-services.ps1 -SkipBuild

Write-Host ""
Write-Success "========================================" 
Write-Success " Quick Start Complete!" 
Write-Success "========================================" 
Write-Host ""
Write-Host "🎉 ImageViewer Platform is now running!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Open your browser: http://localhost:3000" -ForegroundColor White
Write-Host "  2. Login with your credentials" -ForegroundColor White
Write-Host "  3. Create your first library!" -ForegroundColor White
Write-Host ""
Write-Host "Useful Commands:" -ForegroundColor Cyan
Write-Host "  • Check status:      .\status-services.ps1" -ForegroundColor White
Write-Host "  • Monitor status:    .\status-services.ps1 -Watch" -ForegroundColor White
Write-Host "  • Stop services:     .\stop-all-services.ps1" -ForegroundColor White
Write-Host "  • View logs:         Get-Content logs/api/output.log -Wait" -ForegroundColor White
Write-Host ""

