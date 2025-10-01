# PowerShell script to start Image Viewer with PM2
Set-Location "D:\Works\source\image-viewer"

# Check if PM2 is running
$pm2Processes = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object { $_.CommandLine -like "*pm2*" }

if ($pm2Processes) {
    Write-Host "PM2 is already running"
} else {
    Write-Host "Starting PM2..."
    Start-Process -FilePath "pm2" -ArgumentList "start", "ecosystem.config.js" -WindowStyle Hidden
}

Write-Host "Image Viewer should be running on http://localhost:10001"
Write-Host "To check status: pm2 status"
Write-Host "To view logs: pm2 logs image-viewer"

