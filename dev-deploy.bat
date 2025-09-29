@echo off
echo ==========================================
echo    Image Viewer - Development Deployment
echo ==========================================
echo.

echo [1/4] Stopping any running PM2 processes...
call npx pm2 stop image-viewer 2>nul
call npx pm2 delete image-viewer 2>nul

echo.
echo [2/4] Installing dependencies...
call npm run install:all
if %errorlevel% neq 0 (
    echo Error: Failed to install dependencies
    pause
    exit /b 1
)

echo.
echo [3/4] Building frontend...
call npm run build
if %errorlevel% neq 0 (
    echo Error: Failed to build frontend
    pause
    exit /b 1
)

echo.
echo [4/4] Starting server in development mode...
echo.
echo ==========================================
echo    Starting development server...
echo    Press Ctrl+C to stop
echo ==========================================
echo.
call npm start
