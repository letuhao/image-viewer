@echo off
REM ========================================
REM ImageViewer Platform - Stop All Services
REM ========================================
REM Batch file version (most antivirus-friendly)
REM
REM Usage: stop-all-services.bat
REM ========================================

echo ========================================
echo  ImageViewer Platform - Stop Services
echo ========================================
echo.

echo Stopping ImageViewer processes...
echo.

REM Stop .NET processes
taskkill /FI "IMAGENAME eq ImageViewer.Api.exe" /F /T 2>nul
if %ERRORLEVEL%==0 (
    echo [OK] Stopped API Server
) else (
    echo [  ] API Server was not running
)

taskkill /FI "IMAGENAME eq ImageViewer.Worker.exe" /F /T 2>nul
if %ERRORLEVEL%==0 (
    echo [OK] Stopped Worker
) else (
    echo [  ] Worker was not running
)

taskkill /FI "IMAGENAME eq ImageViewer.Scheduler.exe" /F /T 2>nul
if %ERRORLEVEL%==0 (
    echo [OK] Stopped Scheduler
) else (
    echo [  ] Scheduler was not running
)

REM Stop Node.js (Frontend) - be careful not to kill other node processes
echo [  ] Checking for Frontend process...
REM Note: This will close ALL node processes. Be careful if you have other node apps running!
REM Comment out the next line if you have other node processes you want to keep
taskkill /FI "WINDOWTITLE eq ImageViewer Frontend*" /F /T 2>nul
if %ERRORLEVEL%==0 (
    echo [OK] Stopped Frontend
) else (
    echo [  ] Frontend was not running
)

echo.
echo ========================================
echo  Services Stopped
echo ========================================
echo.
echo All ImageViewer services have been stopped.
echo.
echo To start again: start-all-services.bat
echo.
pause

