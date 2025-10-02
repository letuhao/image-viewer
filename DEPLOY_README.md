# ImageViewer Deployment Guide

## Tổng quan
Script `deploy.ps1` được cải thiện để deploy ImageViewer .NET 8 application với error handling tốt hơn và health checks.

## Yêu cầu hệ thống
- .NET 8 SDK hoặc mới hơn
- Entity Framework tools
- PostgreSQL database (tùy chọn)
- PowerShell 5.1 hoặc mới hơn

## Cách sử dụng

### 1. Chuẩn bị môi trường
```powershell
# Set environment variables (tùy chọn)
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=imageviewer;Username=postgres;Password=yourpassword"
$env:TEST_DATA_PATH = "C:\YourTestData"
```

### 2. Chạy deployment
```powershell
# Từ thư mục gốc của project
.\deploy.ps1
```

## Các bước deployment

### [1/5] Stopping existing services
- Dừng các process ImageViewer.Api đang chạy
- Không có lỗi nếu không tìm thấy process

### [2/5] Building solution
- Clean previous build
- Build solution với Release configuration
- Kiểm tra lỗi compilation và dependencies

### [3/5] Running database migrations
- Kiểm tra xem có migrations không
- Apply migrations nếu có
- Skip nếu không có migrations hoặc database context

### [4/5] Starting API server
- Start API server trên ports 11000 (HTTP) và 11001 (HTTPS)
- Health check để đảm bảo server sẵn sàng
- Retry mechanism với timeout

### [5/5] Running integration tests
- Chạy SetupDatabaseTests
- Kiểm tra test data path
- Verify API endpoints

## Cải thiện so với version cũ

### ✅ Error Handling
- Prerequisites checking
- Detailed error messages với troubleshooting tips
- Graceful error recovery

### ✅ Process Management
- Health checks cho API server
- Graceful shutdown với cleanup
- Process tracking

### ✅ Configuration
- Environment variable support
- Flexible test data path
- Database connection validation

### ✅ User Experience
- Colored output với emojis
- Progress indicators
- Helpful error messages
- Cleanup instructions

## Troubleshooting

### Build fails
- Kiểm tra .NET SDK version
- Run `dotnet restore`
- Check compilation errors

### Database migration fails
- Verify database server running
- Check connection string
- Verify database permissions

### API server won't start
- Check port availability (11000, 11001)
- Verify HTTPS certificate
- Check firewall settings

### Integration tests fail
- Set TEST_DATA_PATH environment variable
- Ensure test data folder exists
- Check API server health

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | Database connection string | None |
| `TEST_DATA_PATH` | Path to test data folder | `C:\TestData` |

## Cleanup

Script tự động cleanup khi exit. Để manual cleanup:

```powershell
# Stop API server
Get-Process -Name "dotnet" | Where-Object {$_.MainWindowTitle -like "*ImageViewer*"} | Stop-Process

# Or use Ctrl+C in the API window
```

## Logs

API server logs được lưu trong:
- `src/ImageViewer.Api/logs/`
- Console output của API process

## Support

Nếu gặp vấn đề:
1. Kiểm tra prerequisites
2. Verify environment variables
3. Check error messages chi tiết
4. Review logs
