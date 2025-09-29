@echo off
echo ==========================================
echo    Image Viewer - Deployment Script
echo ==========================================
echo.

echo [1/6] Stopping PM2 processes...
call npx pm2 stop image-viewer
if %errorlevel% neq 0 (
    echo Warning: PM2 process might not be running
)

echo.
echo [2/6] Installing dependencies...
call npm run install:all
if %errorlevel% neq 0 (
    echo Error: Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo [3/6] Building frontend...
call npm run build
if %errorlevel% neq 0 (
    echo Error: Failed to build frontend
    pause
    exit /b 1
)

echo.
echo [4/6] Starting server with PM2...
call npx pm2 start ecosystem.config.js
if %errorlevel% neq 0 (
    echo Error: Failed to start server with PM2
    pause
    exit /b 1
)

echo.
echo [5/6] Checking server status...
timeout /t 3 /nobreak >nul
call npx pm2 status

echo.
echo [6/6] Testing server health...
timeout /t 2 /nobreak >nul
curl -s http://localhost:8081/api/health >nul
if %errorlevel% neq 0 (
    echo Warning: Server health check failed
) else (
    echo ✓ Server is healthy and responding
)

echo.
echo ==========================================
echo    Deployment completed successfully!
echo ==========================================
echo.
echo Server is running on: http://localhost:8081
echo.
echo Useful commands:
echo   PM2 Status:    npx pm2 status
echo   PM2 Logs:      npx pm2 logs image-viewer
echo   PM2 Restart:   npx pm2 restart image-viewer
echo   PM2 Stop:      npx pm2 stop image-viewer
echo.
pause
