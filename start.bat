@echo off
REM Load environment variables
if exist .env (
    for /f "usebackq tokens=1,2 delims==" %%a in (.env) do (
        if not "%%a"=="" if not "%%a:~0,1%"=="#" (
            set "%%a=%%b"
        )
    )
)
REM Set default PORT if not defined
if not defined PORT set PORT=10001

echo Starting Image Viewer Application...

REM Check if PM2 is installed globally
where pm2 >nul 2>nul
if %errorlevel% neq 0 (
    echo PM2 not found globally, using npx...
    npx pm2 start ecosystem.config.js
) else (
    echo Using global PM2 installation...
    pm2 start ecosystem.config.js
)

echo.
echo Application started successfully!
echo.
echo Backend API: http://localhost:%PORT%
echo Frontend: http://localhost:4000 (in development mode)
echo.
echo To view logs: npx pm2 logs image-viewer
echo To stop: npx pm2 stop image-viewer
echo To restart: npx pm2 restart image-viewer
echo To check status: npx pm2 status
echo.
pause
