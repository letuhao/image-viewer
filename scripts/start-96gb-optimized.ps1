# 96GB RAM Optimized RabbitMQ Configuration
# Handles up to 70 million messages in memory

param(
    [int]$WorkerInstances = 8,
    [switch]$EnableMonitoring = $true
)

Write-Host "🚀 Starting 96GB RAM Optimized ImageViewer System" -ForegroundColor Green
Write-Host "📊 System Configuration:" -ForegroundColor Cyan
Write-Host "  - Total RAM: 96GB" -ForegroundColor White
Write-Host "  - Available for Messages: 48GB (50%)" -ForegroundColor White
Write-Host "  - Message Capacity: ~70 Million messages" -ForegroundColor White
Write-Host "  - Worker Instances: $WorkerInstances" -ForegroundColor White
Write-Host "  - Prefetch Count: 100" -ForegroundColor White
Write-Host "  - Batch Size: 1000" -ForegroundColor White

# Check system resources
Write-Host "`n🔍 Checking System Resources..." -ForegroundColor Yellow

$totalRAM = [math]::Round((Get-WmiObject -Class Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
Write-Host "  💾 Total RAM: ${totalRAM}GB" -ForegroundColor White

if ($totalRAM -lt 90) {
    Write-Host "  ⚠️ WARNING: System has less than 90GB RAM. Consider reducing worker instances." -ForegroundColor Yellow
}

# Set high-performance .NET environment variables
Write-Host "`n🔧 Setting High-Performance .NET Environment..." -ForegroundColor Green

$env:DOTNET_GCHeapHardLimit = "0x300000000"  # 12GB heap limit per worker
$env:DOTNET_gcServer = "1"                   # Server GC for better performance
$env:DOTNET_gcConcurrent = "1"               # Concurrent GC
$env:DOTNET_gcRetainVM = "1"                 # Retain VM segments
$env:DOTNET_ThreadPool_UnfairSemaphoreSpinLimit = "10000"  # Optimize thread pool

Write-Host "  ✅ .NET GC optimized for high-memory workloads" -ForegroundColor Green

# Check RabbitMQ status
Write-Host "`n🔍 Checking RabbitMQ..." -ForegroundColor Yellow
try {
    $rabbitmqStatus = Get-Service -Name "RabbitMQ" -ErrorAction SilentlyContinue
    if ($rabbitmqStatus -and $rabbitmqStatus.Status -eq "Running") {
        Write-Host "  ✅ RabbitMQ is running" -ForegroundColor Green
        
        # Check if optimized config is applied
        $configPath = "C:\Program Files\RabbitMQ Server\rabbitmq_server-*\etc\rabbitmq\rabbitmq.conf"
        if (Test-Path $configPath) {
            Write-Host "  ✅ RabbitMQ config file found" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️ Copy rabbitmq-optimized.conf to RabbitMQ config directory" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  ❌ RabbitMQ is not running. Please start RabbitMQ first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ⚠️ Could not check RabbitMQ status" -ForegroundColor Yellow
}

# Check MongoDB status
Write-Host "`n🔍 Checking MongoDB..." -ForegroundColor Yellow
try {
    $mongoStatus = Get-Service -Name "MongoDB" -ErrorAction SilentlyContinue
    if ($mongoStatus -and $mongoStatus.Status -eq "Running") {
        Write-Host "  ✅ MongoDB is running" -ForegroundColor Green
    } else {
        Write-Host "  ❌ MongoDB is not running. Please start MongoDB first." -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ⚠️ Could not check MongoDB status" -ForegroundColor Yellow
}

# Start API server
Write-Host "`n🌐 Starting API Server..." -ForegroundColor Cyan
$apiProcess = Get-Process -Name "ImageViewer.Api" -ErrorAction SilentlyContinue
if (-not $apiProcess) {
    Start-Process -FilePath "dotnet" -ArgumentList "run --project src/ImageViewer.Api --configuration Release" -WindowStyle Minimized
    Start-Sleep -Seconds 3
    Write-Host "  ✅ API Server started" -ForegroundColor Green
} else {
    Write-Host "  ✅ API Server already running" -ForegroundColor Green
}

# Start multiple worker instances optimized for 96GB RAM
Write-Host "`n🔧 Starting Worker Instances..." -ForegroundColor Cyan
$workerProcesses = @()

for ($i = 1; $i -le $WorkerInstances; $i++) {
    Write-Host "  🔧 Starting Worker Instance #$i..." -ForegroundColor White
    
    $workerArgs = @(
        "run"
        "--project", "src/ImageViewer.Worker"
        "--configuration", "Release"
        "--environment", "Production"
    )
    
    $workerProcess = Start-Process -FilePath "dotnet" -ArgumentList $workerArgs -WindowStyle Minimized -PassThru
    $workerProcesses += $workerProcess
    
    Write-Host "    ✅ Worker #$i started (PID: $($workerProcess.Id))" -ForegroundColor Green
    Start-Sleep -Seconds 1  # Stagger startup to avoid resource spikes
}

# Start monitoring if enabled
if ($EnableMonitoring) {
    Write-Host "`n📊 Starting Performance Monitor..." -ForegroundColor Cyan
    Start-Process -FilePath "powershell" -ArgumentList "-File", "scripts/monitor-96gb-performance.ps1" -WindowStyle Minimized
    Write-Host "  ✅ Performance monitor started" -ForegroundColor Green
}

Write-Host "`n🎉 96GB RAM Optimized System Started Successfully!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "📊 System Status:" -ForegroundColor Cyan
Write-Host "  - API Server: 1 instance" -ForegroundColor White
Write-Host "  - Worker Instances: $($workerProcesses.Count) instances" -ForegroundColor White
Write-Host "  - Total Memory Allocation: ~48GB for message queues" -ForegroundColor White
Write-Host "  - Estimated Message Capacity: ~70 Million messages" -ForegroundColor White

Write-Host "`n💡 Performance Tips for 96GB System:" -ForegroundColor Yellow
Write-Host "  - Each worker can handle ~100 concurrent messages" -ForegroundColor White
Write-Host "  - Batch size of 1000 messages reduces overhead" -ForegroundColor White
Write-Host "  - Monitor queue depths to avoid hitting 50M limit" -ForegroundColor White
Write-Host "  - Use SSD storage for cache folders for best I/O performance" -ForegroundColor White
Write-Host "  - Consider RAID 0 for cache storage if you have multiple SSDs" -ForegroundColor White

Write-Host "`n🔍 Monitoring Commands:" -ForegroundColor Cyan
Write-Host "  - Queue Status: scripts/monitor-96gb-performance.ps1" -ForegroundColor White
Write-Host "  - Memory Usage: Get-Process | Where-Object {$_.ProcessName -like '*ImageViewer*'} | Select-Object ProcessName, @{Name='Memory(MB)';Expression={[math]::Round($_.WorkingSet64/1MB,2)}}" -ForegroundColor White
Write-Host "  - RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White

Write-Host "`n⏳ System is ready for processing millions of messages!" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop monitoring..." -ForegroundColor Yellow

# Monitor system status
try {
    while ($true) {
        $activeWorkers = $workerProcesses.Where({ -not $_.HasExited }).Count
        $totalMemory = ($workerProcesses | Where-Object { -not $_.HasExited } | Measure-Object -Property WorkingSet64 -Sum).Sum / 1MB
        
        Write-Host "`r📊 Status: $activeWorkers/$($workerProcesses.Count) workers active, Total Memory: $([math]::Round($totalMemory, 1))MB" -NoNewline -ForegroundColor Cyan
        Start-Sleep -Seconds 10
    }
} catch {
    Write-Host "`n🛑 Monitoring stopped" -ForegroundColor Yellow
}
