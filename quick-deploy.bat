@echo off
echo ==========================================
echo    Image Viewer - Quick Deployment
echo ==========================================
echo.

echo [1/4] Stopping PM2 processes...
call npx pm2 stop image-viewer
if %errorlevel% neq 0 (
    echo Warning: PM2 process might not be running
)

echo.
echo [2/4] Building frontend...
call npm run build
if %errorlevel% neq 0 (
    echo Error: Failed to build frontend
    pause
    exit /b 1
)

echo.
echo [3/4] Starting server with PM2...
call npx pm2 start ecosystem.config.js
if %errorlevel% neq 0 (
    echo Error: Failed to start server with PM2
    pause
    exit /b 1
)

echo.
echo [4/4] Checking server status...
timeout /t 2 /nobreak >nul
call npx pm2 status

echo.
echo ==========================================
echo    Quick deployment completed!
echo ==========================================
echo.
echo Server is running on: http://localhost:8081
echo.
pause
