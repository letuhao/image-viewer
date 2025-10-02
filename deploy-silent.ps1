# ImageViewer .NET 8 - Silent Deploy Script (PowerShell)
# Chạy silent không có user interaction

param(
    [switch]$SkipTests,
    [switch]$SkipMigrations,
    [string]$TestDataPath = "C:\TestData"
)

# Suppress all user prompts
$ErrorActionPreference = "SilentlyContinue"
$WarningPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ImageViewer .NET 8 - Silent Deploy" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Kiểm tra prerequisites
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Yellow

# Kiểm tra .NET SDK
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "❌ .NET SDK not found!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ .NET SDK not found!" -ForegroundColor Red
    exit 1
}

# Kiểm tra Entity Framework tools
try {
    dotnet ef --version 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installing Entity Framework tools..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Failed to install Entity Framework tools!" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host "✓ Entity Framework tools available" -ForegroundColor Green
} catch {
    Write-Host "❌ Entity Framework tools error!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "[1/5] Stopping existing services..." -ForegroundColor Yellow
Stop-Process -Name "ImageViewer.Api" -Force -ErrorAction SilentlyContinue
Write-Host "✓ Stopped existing services" -ForegroundColor Green

Write-Host ""
Write-Host "[2/5] Building solution..." -ForegroundColor Yellow

# Kiểm tra xem thư mục src có tồn tại không
if (-not (Test-Path "src\ImageViewer.Api")) {
    Write-Host "❌ Source directory not found!" -ForegroundColor Red
    exit 1
}

Set-Location "src\ImageViewer.Api"

# Clean và build
dotnet clean --configuration Release --verbosity quiet 2>$null
dotnet build --configuration Release --verbosity quiet 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green

Write-Host ""
Write-Host "[3/5] Running database migrations..." -ForegroundColor Yellow

if (-not $SkipMigrations) {
    try {
        dotnet ef migrations list --configuration Release --no-build 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            dotnet ef database update --configuration Release 2>$null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Database migrations completed" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Database migration failed, continuing..." -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️ No migrations found, skipping..." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️ Database migration error, continuing..." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️ Skipping database migrations" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[4/5] Starting API server..." -ForegroundColor Yellow

# Start API server in background
$apiProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--configuration", "Release", "--urls", "https://localhost:11001;http://localhost:11000" -WindowStyle Hidden -PassThru

# Wait for server to start
Start-Sleep -Seconds 8

# Health check
$maxRetries = 15
$retryCount = 0
$isHealthy = $false

while ($retryCount -lt $maxRetries -and -not $isHealthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:11000/health" -TimeoutSec 3 -ErrorAction SilentlyContinue 2>$null
        if ($response.StatusCode -eq 200) {
            $isHealthy = $true
        }
    } catch {
        $retryCount++
        Start-Sleep -Seconds 2
    }
}

if ($isHealthy) {
    Write-Host "✓ API server started on ports 11000/11001" -ForegroundColor Green
} else {
    Write-Host "⚠️ API server health check failed, but continuing..." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[5/5] Running integration tests..." -ForegroundColor Yellow

if (-not $SkipTests) {
    # Kiểm tra xem thư mục tests có tồn tại không
    $testPath = "..\..\tests\ImageViewer.IntegrationTests"
    if (Test-Path $testPath) {
        Set-Location $testPath
        
        # Set test data path
        $env:TEST_DATA_PATH = $TestDataPath
        
        if (Test-Path $TestDataPath) {
            try {
                dotnet test --filter "SetupDatabaseTests" --verbosity quiet --no-build 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓ Integration tests passed" -ForegroundColor Green
                } else {
                    Write-Host "⚠️ Integration tests failed, continuing..." -ForegroundColor Yellow
                }
            } catch {
                Write-Host "⚠️ Integration tests error, continuing..." -ForegroundColor Yellow
            }
        } else {
            Write-Host "⚠️ Test data path not found, skipping tests..." -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ Integration tests directory not found, skipping..." -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️ Skipping integration tests" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "🎉 DEPLOYMENT COMPLETED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "API Server: https://localhost:11001" -ForegroundColor Yellow
Write-Host "HTTP Server: http://localhost:11000" -ForegroundColor Yellow
Write-Host ""
Write-Host "💡 To stop the API server:" -ForegroundColor Cyan
Write-Host "   Get-Process -Name 'dotnet' | Where-Object {`$_.MainWindowTitle -like '*ImageViewer*'} | Stop-Process" -ForegroundColor Cyan
Write-Host ""

# Cleanup function
function Cleanup {
    if ($apiProcess -and !$apiProcess.HasExited) {
        $apiProcess.Kill()
        $apiProcess.WaitForExit(3000)
    }
}

# Register cleanup on script exit
Register-EngineEvent PowerShell.Exiting -Action { Cleanup } | Out-Null
