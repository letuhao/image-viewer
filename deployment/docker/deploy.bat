@echo off
echo ========================================
echo ImageViewer .NET 8 - Deploy Script
echo ========================================

echo.
echo [1/5] Stopping existing services...
taskkill /F /IM ImageViewer.Api.exe 2>nul
if %errorlevel% equ 0 (
    echo ✓ Stopped existing ImageViewer.Api.exe
) else (
    echo - No existing ImageViewer.Api.exe found
)

echo.
echo [2/5] Building solution...
dotnet build src/ImageViewer.sln --configuration Release --verbosity minimal
if %errorlevel% neq 0 (
    echo ❌ Build failed!
    pause
    exit /b 1
)
echo ✓ Build successful

echo.
echo [3/5] Running database migrations...
cd src/ImageViewer.Api
dotnet ef database update --configuration Release
if %errorlevel% neq 0 (
    echo ❌ Database migration failed!
    pause
    exit /b 1
)
echo ✓ Database migrations completed

echo.
echo [4/5] Starting API server...
start "ImageViewer API" dotnet run --configuration Release --urls "https://localhost:11001;http://localhost:11000"
timeout /t 3 /nobreak >nul
echo ✓ API server started on ports 11000/11001

echo.
echo [5/5] Running integration tests...
cd ..\..\src\tests\ImageViewer.IntegrationTests
dotnet test --filter "SetupDatabaseTests" --verbosity normal
if %errorlevel% neq 0 (
    echo ❌ Integration tests failed!
    pause
    exit /b 1
)
echo ✓ Integration tests passed

echo.
echo ========================================
echo 🎉 DEPLOYMENT COMPLETED SUCCESSFULLY!
echo ========================================
echo.
echo API Server: https://localhost:11001
echo HTTP Server: http://localhost:11000
echo.
echo Press any key to continue...
pause >nul
