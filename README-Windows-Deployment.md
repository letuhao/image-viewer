# 🪟 ImageViewer Platform - Windows Deployment Guide

## 📋 **Overview**

This guide explains how to deploy the ImageViewer Platform on Windows with access to local drives (D:, I:, J:, K:, L:). You have two deployment options:

1. **Docker Deployment** - With Windows drive access
2. **Local Deployment** - Direct .NET execution

## 🐳 **Option 1: Docker Deployment with Windows Drive Access**

### **Prerequisites**
- Docker Desktop for Windows
- Windows drives D:, I:, J:, K:, L: accessible
- PowerShell 5.1 or later

### **Quick Start**
```powershell
# Deploy with Windows drive access
.\deploy-windows-docker.ps1

# Deploy with build
.\deploy-windows-docker.ps1 -Build

# Deploy production environment
.\deploy-windows-docker.ps1 -Environment production -Build
```

### **Windows Drive Mapping**
The Docker containers will have access to your Windows drives through volume mapping:

| Windows Drive | Container Path | Access Level |
|---------------|----------------|--------------|
| D:\ | /app/drives/d | Read/Write |
| I:\ | /app/drives/i | Read/Write |
| J:\ | /app/drives/j | Read/Write |
| K:\ | /app/drives/k | Read/Write |
| L:\ | /app/drives/l | Read/Write |

### **Configuration**
The `docker-compose.windows.yml` file includes:
- Windows drive volume mapping
- Environment variables for drive access
- Health checks for all services
- Network configuration

### **Services**
- **API**: http://localhost:5000
- **Worker**: Background processing
- **MongoDB**: Database
- **RabbitMQ**: Message queue
- **Redis**: Caching (optional)

## 🏠 **Option 2: Local Deployment (No Docker)**

### **Prerequisites**
- .NET 8 SDK
- MongoDB (local installation)
- RabbitMQ (local installation)
- PowerShell 5.1 or later

### **Quick Start**
```powershell
# Build and run locally
.\deploy-local.ps1 -Build -Run

# Stop the application
.\deploy-local.ps1 -Stop
```

### **Configuration**
The `appsettings.Local.json` file includes:
- Local MongoDB connection
- Local RabbitMQ connection
- Windows drive configuration
- Development settings

### **Windows Drive Access**
Local deployment has direct access to all Windows drives:
- D:\
- I:\
- J:\
- K:\
- L:\

## 🔧 **Windows Drive Management API**

### **Available Endpoints**

#### **Get Available Drives**
```http
GET /api/v1/windowsdrives
```

#### **Get Drive Information**
```http
GET /api/v1/windowsdrives/{driveLetter}
```

#### **Check Drive Accessibility**
```http
GET /api/v1/windowsdrives/{driveLetter}/accessible
```

#### **Scan Drive for Media Files**
```http
POST /api/v1/windowsdrives/{driveLetter}/scan
Content-Type: application/json

{
  "extensions": [".jpg", ".png", ".mp4"]
}
```

#### **Get Directory Structure**
```http
GET /api/v1/windowsdrives/{driveLetter}/directories?path=subfolder
```

#### **Create Library from Drive**
```http
POST /api/v1/windowsdrives/{driveLetter}/library
Content-Type: application/json

{
  "libraryName": "My Photo Library",
  "description": "Photos from drive D"
}
```

#### **Start/Stop Drive Monitoring**
```http
POST /api/v1/windowsdrives/{driveLetter}/monitor/start
POST /api/v1/windowsdrives/{driveLetter}/monitor/stop
```

#### **Get Drive Statistics**
```http
GET /api/v1/windowsdrives/{driveLetter}/statistics
```

## 📁 **File Structure**

```
image-viewer/
├── docker-compose.windows.yml          # Docker config with Windows drives
├── deploy-windows-docker.ps1           # Windows Docker deployment script
├── deploy-local.ps1                    # Local deployment script
├── appsettings.Local.json              # Local configuration
├── src/
│   ├── ImageViewer.Api/
│   │   └── Controllers/
│   │       └── WindowsDrivesController.cs  # Windows drive API
│   └── ImageViewer.Application/
│       └── Services/
│           ├── IWindowsDriveService.cs     # Windows drive service interface
│           └── WindowsDriveService.cs      # Windows drive service implementation
└── README-Windows-Deployment.md        # This file
```

## 🚀 **Deployment Steps**

### **Docker Deployment**
1. Ensure Docker Desktop is running
2. Verify Windows drives are accessible
3. Run deployment script:
   ```powershell
   .\deploy-windows-docker.ps1 -Build
   ```
4. Wait for services to be healthy
5. Access API at http://localhost:5000

### **Local Deployment**
1. Install .NET 8 SDK
2. Install and start MongoDB
3. Install and start RabbitMQ
4. Run deployment script:
   ```powershell
   .\deploy-local.ps1 -Build -Run
   ```
5. Access API at http://localhost:5000

## 🔍 **Verification**

### **Check Service Status**
```powershell
# Docker deployment
docker-compose ps

# Local deployment
Get-Process -Name "ImageViewer.*"
```

### **Test Drive Access**
```powershell
# Test API endpoint
Invoke-RestMethod -Uri "http://localhost:5000/api/v1/windowsdrives" -Method GET
```

### **Check Health**
```powershell
# API health check
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET
```

## 🛠️ **Troubleshooting**

### **Docker Issues**
- **Drive not accessible**: Check if drive exists and is mounted
- **Permission denied**: Run PowerShell as Administrator
- **Container fails to start**: Check Docker Desktop is running

### **Local Deployment Issues**
- **MongoDB connection failed**: Ensure MongoDB service is running
- **RabbitMQ connection failed**: Ensure RabbitMQ service is running
- **Port already in use**: Change ports in configuration

### **Drive Access Issues**
- **Drive not found**: Verify drive letter exists
- **Access denied**: Check file permissions
- **Path too long**: Use shorter paths or enable long path support

## 📊 **Performance Considerations**

### **Docker Deployment**
- Volume mapping has slight performance overhead
- Use SSD drives for better performance
- Monitor disk I/O usage

### **Local Deployment**
- Direct file system access
- Better performance for large files
- Lower resource usage

## 🔒 **Security Notes**

- Windows drives are mounted with read/write access
- Ensure proper file permissions
- Consider using read-only access for sensitive drives
- Monitor file system changes

## 📝 **Configuration Examples**

### **Environment Variables**
```json
{
  "FileStorage": {
    "BasePath": "D:\\ImageViewerData",
    "WindowsDrives": {
      "D": "D:\\",
      "I": "I:\\",
      "J": "J:\\",
      "K": "K:\\",
      "L": "L:\\"
    }
  }
}
```

### **Docker Volume Mapping**
```yaml
volumes:
  - D:/:/app/drives/d:rw
  - I:/:/app/drives/i:rw
  - J:/:/app/drives/j:rw
  - K:/:/app/drives/k:rw
  - L:/:/app/drives/l:rw
```

## 🎯 **Next Steps**

1. **Deploy the system** using your preferred method
2. **Test drive access** using the API endpoints
3. **Create libraries** from your drives
4. **Set up monitoring** for file changes
5. **Configure caching** for better performance

## 📞 **Support**

If you encounter issues:
1. Check the logs in the `logs/` directory
2. Verify all prerequisites are installed
3. Ensure Windows drives are accessible
4. Check service health endpoints

---

**Happy deploying! 🚀**
