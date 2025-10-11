# ========================================
# ImageViewer Platform - Service Status
# ========================================
# This script checks the status of all ImageViewer services
#
# Usage:
#   .\status-services.ps1
#   .\status-services.ps1 -Watch  (continuous monitoring)
# ========================================

param(
    [switch]$Watch = $false,
    [int]$RefreshInterval = 5  # seconds
)

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Get-ServiceStatus {
    Clear-Host
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host " ImageViewer Platform - Service Status" -ForegroundColor Cyan
    Write-Host " $timestamp" -ForegroundColor Gray
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Check backend processes
    Write-Host "Backend Services:" -ForegroundColor Yellow
    Write-Host ""
    
    $services = @(
        @{Name="API Server"; Process="ImageViewer.Api"; Port=11001; Url="https://localhost:11001/api/v1/health"},
        @{Name="Worker"; Process="ImageViewer.Worker"; Port=$null; Url=$null},
        @{Name="Scheduler"; Process="ImageViewer.Scheduler"; Port=$null; Url=$null}
    )
    
    $runningCount = 0
    
    foreach ($svc in $services) {
        $process = Get-Process -Name $svc.Process -ErrorAction SilentlyContinue
        
        if ($process) {
            $runningCount++
            $uptime = (Get-Date) - $process.StartTime
            $uptimeStr = "{0:D2}:{1:D2}:{2:D2}" -f $uptime.Hours, $uptime.Minutes, $uptime.Seconds
            
            Write-Host "  ✓ " -ForegroundColor Green -NoNewline
            Write-Host "$($svc.Name)".PadRight(20) -NoNewline
            Write-Host "Running".PadRight(12) -ForegroundColor Green -NoNewline
            Write-Host "PID: $($process.Id)".PadRight(15) -NoNewline
            Write-Host "Uptime: $uptimeStr" -ForegroundColor Gray
            
            # Check health endpoint if available
            if ($svc.Url) {
                try {
                    $response = Invoke-WebRequest -Uri $svc.Url -Method GET -TimeoutSec 2 -SkipCertificateCheck -ErrorAction SilentlyContinue
                    if ($response.StatusCode -eq 200) {
                        Write-Host "      → Health check: PASSED" -ForegroundColor Green
                    }
                } catch {
                    Write-Host "      → Health check: FAILED" -ForegroundColor Red
                }
            }
        } else {
            Write-Host "  ✗ " -ForegroundColor Red -NoNewline
            Write-Host "$($svc.Name)".PadRight(20) -NoNewline
            Write-Host "Not Running" -ForegroundColor Red
        }
    }
    
    Write-Host ""
    
    # Check Frontend
    Write-Host "Frontend:" -ForegroundColor Yellow
    Write-Host ""
    
    $nodeProcesses = Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
        try {
            $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue).CommandLine
            $cmdLine -match "image-viewer\\client" -or $cmdLine -match "vite"
        } catch {
            $false
        }
    }
    
    if ($nodeProcesses) {
        foreach ($proc in $nodeProcesses) {
            $uptime = (Get-Date) - $proc.StartTime
            $uptimeStr = "{0:D2}:{1:D2}:{2:D2}" -f $uptime.Hours, $uptime.Minutes, $uptime.Seconds
            
            Write-Host "  ✓ " -ForegroundColor Green -NoNewline
            Write-Host "Frontend (Vite)".PadRight(20) -NoNewline
            Write-Host "Running".PadRight(12) -ForegroundColor Green -NoNewline
            Write-Host "PID: $($proc.Id)".PadRight(15) -NoNewline
            Write-Host "Uptime: $uptimeStr" -ForegroundColor Gray
            
            # Check frontend URL
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:3000" -Method GET -TimeoutSec 2 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host "      → URL: http://localhost:3000 (ACCESSIBLE)" -ForegroundColor Green
                }
            } catch {
                Write-Host "      → URL: http://localhost:3000 (NOT READY)" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "  ✗ " -ForegroundColor Red -NoNewline
        Write-Host "Frontend (Vite)".PadRight(20) -NoNewline
        Write-Host "Not Running" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # External Dependencies
    Write-Host "External Dependencies:" -ForegroundColor Yellow
    Write-Host ""
    
    # MongoDB
    $mongoTest = Test-NetConnection -ComputerName localhost -Port 27017 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($mongoTest.TcpTestSucceeded) {
        Write-Host "  ✓ MongoDB         Running on port 27017" -ForegroundColor Green
    } else {
        Write-Host "  ✗ MongoDB         Not detected on port 27017" -ForegroundColor Red
    }
    
    # RabbitMQ
    $rabbitTest = Test-NetConnection -ComputerName localhost -Port 5672 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($rabbitTest.TcpTestSucceeded) {
        Write-Host "  ✓ RabbitMQ        Running on port 5672" -ForegroundColor Green
    } else {
        Write-Host "  ✗ RabbitMQ        Not detected on port 5672" -ForegroundColor Red
    }
    
    # Redis
    $redisTest = Test-NetConnection -ComputerName localhost -Port 6379 -WarningAction SilentlyContinue -InformationLevel Quiet
    if ($redisTest.TcpTestSucceeded) {
        Write-Host "  ✓ Redis           Running on port 6379" -ForegroundColor Green
    } else {
        Write-Host "  ✗ Redis           Not detected on port 6379" -ForegroundColor Red
    }
    
    Write-Host ""
    
    # Summary
    $totalServices = 4  # API, Worker, Scheduler, Frontend
    Write-Host "Summary: $runningCount/$totalServices services running" -ForegroundColor Cyan
    Write-Host ""
    
    if ($runningCount -eq 0) {
        Write-Warning "No services running. Start them with: .\start-all-services.ps1"
    } elseif ($runningCount -lt $totalServices) {
        Write-Warning "Some services are not running. Check logs for errors."
    } else {
        Write-Success "All services operational!"
    }
    
    Write-Host ""
}

# Main execution
if ($Watch) {
    Write-Info "Continuous monitoring mode (Ctrl+C to exit)"
    Write-Host ""
    
    try {
        while ($true) {
            Get-ServiceStatus
            Write-Host "Refreshing in $RefreshInterval seconds..." -ForegroundColor Gray
            Start-Sleep -Seconds $RefreshInterval
        }
    } catch {
        Write-Host ""
        Write-Info "Monitoring stopped"
    }
} else {
    Get-ServiceStatus
}

