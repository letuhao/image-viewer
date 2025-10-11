#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Restart the frontend dev server

.DESCRIPTION
    Stops the current frontend dev server and restarts it to apply code changes
#>

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  Restart Frontend Dev Server" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Kill existing node processes on port 3000
Write-Host "[INFO] Stopping frontend dev server..." -ForegroundColor Cyan

$processes = Get-Process node -ErrorAction SilentlyContinue | Where-Object {
    $netstat = netstat -ano | Select-String ":3000" | Select-String "LISTENING"
    $portProcess = $netstat | ForEach-Object { 
        if ($_ -match "\s+(\d+)$") { 
            [int]$matches[1] 
        } 
    }
    $portProcess -contains $_.Id
}

if ($processes) {
    $processes | ForEach-Object {
        Write-Host "  Stopping process $($_.Id)..." -ForegroundColor Yellow
        Stop-Process -Id $_.Id -Force
    }
    Write-Host "[SUCCESS] Frontend stopped" -ForegroundColor Green
    Start-Sleep -Seconds 2
} else {
    Write-Host "[INFO] No frontend process found on port 3000" -ForegroundColor Yellow
}

# Start frontend
Write-Host ""
Write-Host "[INFO] Starting frontend dev server..." -ForegroundColor Cyan
Write-Host ""

Set-Location -Path "$PSScriptRoot\client"
Start-Process powershell -ArgumentList "-NoExit", "-Command", "npm run dev" -WindowStyle Normal

Write-Host ""
Write-Host "[SUCCESS] Frontend dev server starting..." -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Wait for dev server to start (usually 2-3 seconds)" -ForegroundColor Gray
Write-Host "  2. Open browser: http://localhost:3000" -ForegroundColor Gray
Write-Host "  3. Hard refresh: Ctrl+Shift+R or Ctrl+F5" -ForegroundColor Gray
Write-Host "  4. Open DevTools (F12) and check Network tab" -ForegroundColor Gray
Write-Host "  5. Login and try creating a library" -ForegroundColor Gray
Write-Host "  6. Check request headers - should now include Authorization" -ForegroundColor Gray
Write-Host ""

