# 🚀 ImageViewer Platform - Local Deployment Scripts

PowerShell scripts for running ImageViewer platform locally without Docker.

---

## 📋 Available Scripts

### **PowerShell Scripts** (Full-featured)

| Script | Purpose | Usage |
|--------|---------|-------|
| **quick-start.ps1** | First-time setup + start | `.\quick-start.ps1` |
| **start-all-services.ps1** | Start all services (hidden) | `.\start-all-services.ps1` |
| **start-all-services.ps1 -Visible** | Start with visible windows | `.\start-all-services.ps1 -Visible` |
| **start-all-services-safe.ps1** | Antivirus-friendly version | `.\start-all-services-safe.ps1` |
| **stop-all-services.ps1** | Stop all services | `.\stop-all-services.ps1` |
| **status-services.ps1** | Check service status | `.\status-services.ps1` |
| **view-logs.ps1** | View service logs | `.\view-logs.ps1` |

### **Batch Scripts** (Maximum compatibility)

| Script | Purpose | Usage |
|--------|---------|-------|
| **start-all-services.bat** | Start all services (CMD) | `start-all-services.bat` |
| **stop-all-services.bat** | Stop all services (CMD) | `stop-all-services.bat` |

---

## 🛡️ **Antivirus Issues?**

If PowerShell scripts are blocked:

1. **Use .bat version** (works on all systems):
   ```batch
   start-all-services.bat
   ```

2. **Use -Visible flag** (PowerShell, shows windows):
   ```powershell
   .\start-all-services.ps1 -Visible
   ```

3. **Add exception** (best UX, requires admin):
   ```
   Windows Security → Exclusions → Add folder → image-viewer
   ```

See **ANTIVIRUS_GUIDE.md** for complete troubleshooting.

---

## 🎯 Quick Start (First Time)

### **1. One-Command Setup**

```powershell
.\quick-start.ps1
```

**What it does**:
1. ✅ Checks prerequisites (.NET 9, Node.js, MongoDB, RabbitMQ, Redis)
2. ✅ Restores NuGet packages
3. ✅ Installs npm dependencies
4. ✅ Builds all projects (Release mode)
5. ✅ Starts all services in background
6. ✅ Performs health checks

**Time**: ~5-10 minutes (first run, including npm install)

**Output**: All services running, ready to use!

---

## 🔄 Daily Usage

### **Start Services**

```powershell
.\start-all-services.ps1
```

**Features**:
- Stops existing processes automatically
- Starts all 4 services (API, Worker, Scheduler, Frontend)
- Runs in background (silent mode)
- Health checks with retries
- Comprehensive logging to files
- Process monitoring

**Time**: ~20-30 seconds

**Options**:
```powershell
.\start-all-services.ps1 -SkipBuild   # Don't rebuild, just run
.\start-all-services.ps1 -Verbose     # Show detailed output
```

### **Stop Services**

```powershell
.\stop-all-services.ps1
```

**Features**:
- Graceful shutdown (tries CloseMainWindow first)
- Force kill if graceful fails
- Stops all 4 services
- Cleans up hung processes
- Confirms all stopped

**Options**:
```powershell
.\stop-all-services.ps1 -Force   # Immediate force kill
```

### **Check Status**

```powershell
.\status-services.ps1
```

**Shows**:
- ✅ Running/Stopped status for each service
- Process ID (PID)
- Uptime
- Health check results
- External dependencies (MongoDB, RabbitMQ, Redis)

**Options**:
```powershell
.\status-services.ps1 -Watch              # Continuous monitoring
.\status-services.ps1 -Watch -RefreshInterval 3   # Refresh every 3 seconds
```

### **View Logs**

```powershell
.\view-logs.ps1
```

**Interactive menu**:
```
Select a service to view logs:
  1. API Server
  2. Worker
  3. Scheduler
  4. Frontend
  5. All (combined)
```

**Direct usage**:
```powershell
.\view-logs.ps1 -Service api              # Last 50 lines
.\view-logs.ps1 -Service api -Follow      # Tail/follow logs
.\view-logs.ps1 -Service worker -Lines 100  # Last 100 lines
.\view-logs.ps1 -All                      # All services combined
```

---

## 📁 File Structure

```
image-viewer/
├── quick-start.ps1              # First-time setup
├── start-all-services.ps1       # Start services
├── stop-all-services.ps1        # Stop services
├── status-services.ps1          # Check status
├── view-logs.ps1                # View logs
└── logs/                        # Created automatically
    ├── api/
    │   ├── output.log           # API stdout
    │   └── error.log            # API stderr
    ├── worker/
    │   ├── output.log
    │   └── error.log
    ├── scheduler/
    │   ├── output.log
    │   └── error.log
    └── frontend/
        ├── output.log
        └── error.log
```

---

## 🔧 Service Details

### **Services Started**

| Service | Port | Logs | Purpose |
|---------|------|------|---------|
| **API Server** | 11001 | logs/api/ | REST API, Hangfire Dashboard |
| **Worker** | - | logs/worker/ | RabbitMQ consumers, image processing |
| **Scheduler** | - | logs/scheduler/ | Hangfire server, job execution |
| **Frontend** | 3000 | logs/frontend/ | React development server |

### **Process Names**

| Service | Process Name | Window |
|---------|--------------|--------|
| API | ImageViewer.Api.exe | Hidden |
| Worker | ImageViewer.Worker.exe | Hidden |
| Scheduler | ImageViewer.Scheduler.exe | Hidden |
| Frontend | node.exe (vite) | Hidden |

---

## 🌐 Access URLs

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | User login required |
| **API** | https://localhost:11001 | JWT token required |
| **Swagger** | https://localhost:11001/swagger | Open |
| **Hangfire** | https://localhost:11001/hangfire | Auth required (Admin/Scheduler) |
| **RabbitMQ** | http://localhost:15672 | guest/guest |

---

## 🛠️ Troubleshooting

### **Services Won't Start**

**Check prerequisites**:
```powershell
dotnet --version    # Should be 9.x
node --version      # Should be 18+ or 20+
```

**Check external services**:
```powershell
Test-NetConnection localhost -Port 27017  # MongoDB
Test-NetConnection localhost -Port 5672   # RabbitMQ
Test-NetConnection localhost -Port 6379   # Redis
```

**Check port conflicts**:
```powershell
# Check if ports are in use
Get-NetTCPConnection -LocalPort 11001     # API
Get-NetTCPConnection -LocalPort 3000      # Frontend
```

### **Service Crashes Immediately**

**View error logs**:
```powershell
Get-Content logs/api/error.log -Tail 50
Get-Content logs/worker/error.log -Tail 50
Get-Content logs/scheduler/error.log -Tail 50
```

**Common issues**:
1. **MongoDB not running**: Start MongoDB service
2. **RabbitMQ not running**: Start RabbitMQ or use Docker
3. **Port conflict**: Kill process using port 11001 or 3000
4. **Missing dependencies**: Run `.\quick-start.ps1` again

### **Build Failures**

**Clean and rebuild**:
```powershell
# Clean
dotnet clean src/ImageViewer.sln
Remove-Item -Recurse -Force src/*/bin, src/*/obj

# Rebuild
dotnet build src/ImageViewer.sln -c Release
```

### **Frontend Not Loading**

**Check logs**:
```powershell
Get-Content logs/frontend/output.log -Tail 50
```

**Common fixes**:
```powershell
# Clear cache and reinstall
cd client
Remove-Item -Recurse -Force node_modules, dist
npm install
cd ..
.\start-all-services.ps1
```

---

## 💡 Tips & Tricks

### **Restart Single Service**

```powershell
# Stop specific service
Get-Process ImageViewer.Api | Stop-Process -Force

# Start it again
cd src/ImageViewer.Api
Start-Process dotnet -ArgumentList "run -c Release" -WindowStyle Hidden
cd ../..
```

### **View Live Logs (Multiple Terminals)**

**Terminal 1 - API**:
```powershell
Get-Content logs/api/output.log -Wait
```

**Terminal 2 - Worker**:
```powershell
Get-Content logs/worker/output.log -Wait
```

**Terminal 3 - Scheduler**:
```powershell
Get-Content logs/scheduler/output.log -Wait
```

### **Quick Status Check**

```powershell
Get-Process | Where-Object {$_.ProcessName -like "*ImageViewer*"} | 
    Select-Object ProcessName, Id, StartTime, WorkingSet64 | 
    Format-Table -AutoSize
```

### **Monitor Resource Usage**

```powershell
while ($true) {
    Clear-Host
    Get-Process ImageViewer* | 
        Select-Object ProcessName, 
                      @{L='CPU(%)';E={$_.CPU}},
                      @{L='Memory(MB)';E={[math]::Round($_.WorkingSet64/1MB,2)}} |
        Format-Table -AutoSize
    Start-Sleep -Seconds 5
}
```

---

## 📊 Expected Resource Usage

| Service | CPU | Memory | Notes |
|---------|-----|--------|-------|
| API | 5-15% | 150-300MB | Spikes during requests |
| Worker | 10-30% | 200-400MB | High during image processing |
| Scheduler | <5% | 100-200MB | Idle most of the time |
| Frontend | 5-10% | 100-200MB | Vite dev server |

**Total**: ~500MB-1GB RAM, ~30% CPU average

---

## 🔄 Development Workflow

### **Morning Routine**

```powershell
# Start everything
.\quick-start.ps1

# Check status
.\status-services.ps1

# Open browser: http://localhost:3000
```

### **During Development**

```powershell
# Code changes → Services auto-reload (hot reload enabled)

# If manual restart needed:
.\stop-all-services.ps1
.\start-all-services.ps1 -SkipBuild  # Fast restart
```

### **End of Day**

```powershell
.\stop-all-services.ps1
```

---

## 🐛 Debugging

### **Enable Verbose Logging**

**API** (appsettings.Development.json):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

**Worker** (appsettings.json):
```json
{
  "Logging": {
    "LogLevel": {
      "ImageViewer.Worker": "Debug"
    }
  }
}
```

### **View Specific Error Logs**

```powershell
# API errors only
Get-Content logs/api/error.log -Tail 50

# Worker errors
Get-Content logs/worker/error.log -Tail 50

# Scheduler errors
Get-Content logs/scheduler/error.log -Tail 50
```

### **Search Logs for Errors**

```powershell
# Find all errors in API logs
Select-String -Path logs/api/output.log -Pattern "error|exception|fail" -Context 2

# Find scheduler job registrations
Select-String -Path logs/scheduler/output.log -Pattern "Registered new job"

# Find worker image processing
Select-String -Path logs/worker/output.log -Pattern "Processing.*image"
```

---

## 📝 Common Tasks

### **Create Test Library**

```powershell
# 1. Start services
.\start-all-services.ps1

# 2. Wait for API to be ready (30 seconds)

# 3. Create library via API
$token = "YOUR_JWT_TOKEN"
$body = @{
    name = "Test Library"
    path = "D:\Test\Photos"
    ownerId = "USER_ID"
    description = "Test library for development"
    autoScan = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://localhost:11001/api/v1/libraries" `
    -Method POST `
    -Body $body `
    -ContentType "application/json" `
    -Headers @{Authorization="Bearer $token"} `
    -SkipCertificateCheck
```

### **Monitor Scheduler Jobs**

```powershell
# Watch scheduler logs
.\view-logs.ps1 -Service scheduler -Follow

# Look for:
# "✅ Registered new job: Library Scan - Test Library"
```

### **Trigger Manual Library Scan**

```powershell
# Go to Hangfire dashboard
Start-Process "https://localhost:11001/hangfire"

# Navigate to: Recurring Jobs
# Find: "scheduled-job-{ID}"
# Click: "Trigger now"
```

---

## ✅ Verification Checklist

After running `.\start-all-services.ps1`:

- [ ] API responding at https://localhost:11001
- [ ] Swagger UI accessible at https://localhost:11001/swagger
- [ ] Hangfire dashboard at https://localhost:11001/hangfire
- [ ] Frontend loading at http://localhost:3000
- [ ] No errors in logs/*/error.log files
- [ ] All 4 processes visible in Task Manager
- [ ] MongoDB connection successful (check API logs)
- [ ] RabbitMQ connection successful (check Worker logs)
- [ ] Redis connection successful (check API logs)

---

## 🎓 Best Practices

1. **Use quick-start.ps1 for first-time setup**
2. **Use start-all-services.ps1 for daily work**
3. **Always stop services before system shutdown**
4. **Monitor logs during development**
5. **Use status-services.ps1 -Watch for health monitoring**
6. **Check error logs if services crash**

---

## 🆘 Emergency Commands

### **Force Kill Everything**

```powershell
Get-Process ImageViewer* | Stop-Process -Force
Get-Process node | Where-Object {
    (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine -match "vite"
} | Stop-Process -Force
```

### **Clean Everything**

```powershell
# Stop services
.\stop-all-services.ps1 -Force

# Clean builds
dotnet clean src/ImageViewer.sln
Remove-Item -Recurse -Force src/*/bin, src/*/obj

# Clean frontend
Remove-Item -Recurse -Force client/node_modules, client/dist

# Clean logs
Remove-Item -Recurse -Force logs/*

# Rebuild from scratch
.\quick-start.ps1
```

---

**Happy Developing!** 🎉

