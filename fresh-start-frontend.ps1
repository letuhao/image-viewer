#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fresh start of frontend with cache cleared

.DESCRIPTION
    Stops frontend, clears all caches, and starts fresh
#>

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Fresh Frontend Start (Cache Cleared)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Stop frontend
Write-Host "[1/4] Stopping frontend..." -ForegroundColor Yellow
Get-Process node -ErrorAction SilentlyContinue | Where-Object {
    $_.CommandLine -like "*vite*" -or $_.Path -like "*client*"
} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Write-Host "  ✓ Stopped" -ForegroundColor Green

# Step 2: Clear caches
Write-Host "[2/4] Clearing caches..." -ForegroundColor Yellow
Set-Location "$PSScriptRoot\client"
Remove-Item -Path "dist" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "node_modules\.vite" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path ".vite" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "  ✓ Cache cleared" -ForegroundColor Green

# Step 3: Start fresh
Write-Host "[3/4] Starting Vite dev server..." -ForegroundColor Yellow
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$PSScriptRoot\client'; npm run dev" -WindowStyle Normal
Write-Host "  ✓ Started in new window" -ForegroundColor Green

# Step 4: Instructions
Write-Host "[4/4] Next steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Wait 5-10 seconds for Vite to start" -ForegroundColor Gray
Write-Host "  2. Open: http://localhost:3000" -ForegroundColor Gray
Write-Host "  3. Open DevTools Console (F12)" -ForegroundColor Gray
Write-Host "  4. Login to the application" -ForegroundColor Gray
Write-Host "  5. Navigate to Libraries page" -ForegroundColor Gray
Write-Host "  6. Try creating a library" -ForegroundColor Gray
Write-Host ""
Write-Host "  Expected in Console:" -ForegroundColor Cyan
Write-Host "    [API Interceptor] Request URL: /libraries" -ForegroundColor Gray
Write-Host "    [API Interceptor] Token from localStorage: eyJ..." -ForegroundColor Gray
Write-Host "    [API Interceptor] Authorization header added" -ForegroundColor Gray
Write-Host ""
Write-Host "  Expected in Network tab:" -ForegroundColor Cyan
Write-Host "    Request to: http://localhost:3000/api/v1/libraries (NOT https://localhost:11001)" -ForegroundColor Gray
Write-Host "    Header: authorization: Bearer ..." -ForegroundColor Gray
Write-Host "    Header: x-debug-interceptor: active" -ForegroundColor Gray
Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "  Fresh start complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

