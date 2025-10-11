@echo off
REM ========================================
REM ImageViewer Platform - Start All Services
REM ========================================
REM Batch file version (most antivirus-friendly)
REM No PowerShell required
REM
REM Usage: start-all-services.bat
REM ========================================

echo ========================================
echo  ImageViewer Platform - Service Launcher
echo ========================================
echo.

REM Check if running from correct directory
if not exist "src\ImageViewer.sln" (
    echo ERROR: Must run from image-viewer root directory
    echo Current directory: %CD%
    pause
    exit /b 1
)

echo [OK] Running from correct directory
echo.

REM Create log directories
echo Creating log directories...
if not exist "logs\api" mkdir logs\api
if not exist "logs\worker" mkdir logs\worker
if not exist "logs\scheduler" mkdir logs\scheduler
if not exist "logs\frontend" mkdir logs\frontend
echo.

REM Start services in separate windows
echo Starting services...
echo.

echo [1/4] Starting API Server (port 11001)...
start "ImageViewer API" /D "%CD%\src\ImageViewer.Api" cmd /k "dotnet run -c Release > ..\..\logs\api\output.log 2>&1"
timeout /t 5 /nobreak > nul

echo [2/4] Starting Worker...
start "ImageViewer Worker" /D "%CD%\src\ImageViewer.Worker" cmd /k "dotnet run -c Release > ..\..\logs\worker\output.log 2>&1"
timeout /t 3 /nobreak > nul

echo [3/4] Starting Scheduler...
start "ImageViewer Scheduler" /D "%CD%\src\ImageViewer.Scheduler" cmd /k "dotnet run -c Release > ..\..\logs\scheduler\output.log 2>&1"
timeout /t 3 /nobreak > nul

echo [4/4] Starting Frontend (port 3000)...
start "ImageViewer Frontend" /D "%CD%\client" cmd /k "npm run dev > ..\logs\frontend\output.log 2>&1"

echo.
echo ========================================
echo  All Services Started Successfully!
echo ========================================
echo.
echo You should see 4 console windows:
echo   1. ImageViewer API
echo   2. ImageViewer Worker
echo   3. ImageViewer Scheduler
echo   4. ImageViewer Frontend
echo.
echo Access URLs:
echo   Frontend:  http://localhost:3000
echo   API:       https://localhost:11001
echo   Swagger:   https://localhost:11001/swagger
echo   Hangfire:  https://localhost:11001/hangfire
echo.
echo Logs saved to: logs\
echo.
echo To stop: Close each console window, or run stop-all-services.bat
echo.
pause

