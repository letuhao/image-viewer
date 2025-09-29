@echo off
echo Stopping Image Viewer Application...

REM Check if PM2 is installed globally
where pm2 >nul 2>nul
if %errorlevel% neq 0 (
    echo PM2 not found globally, using npx...
    npx pm2 stop image-viewer
) else (
    echo Using global PM2 installation...
    pm2 stop image-viewer
)

echo.
echo Application stopped successfully!
echo.
pause
