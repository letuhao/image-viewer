@echo off
echo ZIP Recovery Tool Test Script
echo ================================

REM Check if the tool is built
if not exist "bin\Debug\net9.0\ImageViewer.Tools.ZipRecover.exe" (
    echo Building ZIP Recovery Tool...
    dotnet build
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
)

REM Check if input file exists
if not exist "data\input.txt" (
    echo Input file not found: data\input.txt
    echo Please ensure the input file exists with log entries containing ZIP file paths.
    pause
    exit /b 1
)

REM Check if 7-Zip is available
if exist "C:\Program Files\7-Zip\7z.exe" (
    echo [OK] 7-Zip found at: C:\Program Files\7-Zip\7z.exe
) else (
    echo [WARN] 7-Zip not found at: C:\Program Files\7-Zip\7z.exe
    echo        Tool will fall back to .NET ZipFile
)

REM Create backup directory if it doesn't exist
if not exist "backup_corrupted" (
    mkdir "backup_corrupted" 2>nul
    echo [OK] Created backup directory: backup_corrupted
)

REM Create temp directory if it doesn't exist
if not exist "temp_recovery" (
    mkdir "temp_recovery" 2>nul
    echo [OK] Created temp directory: temp_recovery
)

echo.
echo Running ZIP Recovery Tool...
echo =============================

REM Run the tool
dotnet run

echo.
echo ZIP Recovery Tool completed!
echo Check the logs above for results.
echo.
echo Files processed:
echo - Original corrupted ZIPs: Moved to backup_corrupted\
echo - Recovered ZIPs: Replaced original files
echo - Temp files: Automatically cleaned up
echo.
pause
