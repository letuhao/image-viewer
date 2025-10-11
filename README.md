# 🖼️ ImageViewer Platform

> A modern, high-performance image management platform with advanced library organization, automated scanning, and intelligent caching. Built with .NET 9, MongoDB, RabbitMQ, and React.

[![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![MongoDB](https://img.shields.io/badge/MongoDB-7.0-47A248?logo=mongodb)](https://www.mongodb.com/)
[![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.13-FF6600?logo=rabbitmq)](https://www.rabbitmq.com/)
[![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?logo=typescript)](https://www.typescriptlang.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## ✨ Features

### 📚 **Library Management**
- **Multi-Library Support**: Organize collections into separate libraries
- **Auto-Scan Scheduling**: Cron-based automatic library scanning with Hangfire
- **Nested Collection Discovery**: Recursive folder scanning with compressed file support
- **Library Statistics**: Real-time tracking of collections, media items, and total size
- **Access Control**: Public/private libraries with owner-based permissions

### 🗂️ **Collection Management**
- **Folder Collections**: Scan directories for images and videos
- **Archive Support**: ZIP, 7Z, RAR, CBZ, CBR, TAR formats
- **Nested Collections**: Multi-level folder hierarchies
- **Bulk Operations**: Add, scan, and process thousands of collections
- **Auto-Discovery**: Intelligent collection detection during library scans

### 🖼️ **Advanced Image Viewer**
- **View Modes**: Single, Double, Triple, Quad-view layouts
- **Navigation Modes**: Paging or continuous scroll
- **Cross-Collection Navigation**: Seamless browsing across collections
- **Zoom & Pan**: Mouse wheel zoom (Ctrl+Wheel in scroll mode)
- **Slideshow**: Auto-advance with configurable intervals
- **Keyboard Shortcuts**: Full keyboard control
- **Random Collection**: Quick discovery with Ctrl+R

### ⚡ **Background Processing**
- **Distributed Architecture**: Separate API, Worker, and Scheduler services
- **RabbitMQ Message Queue**: Asynchronous task processing
- **Hangfire Scheduler**: Cron-based recurring jobs
- **Multi-Stage Jobs**: Scan → Process → Thumbnail → Cache pipeline
- **Concurrent Processing**: Handle millions of files efficiently
- **Atomic Statistics**: Race-condition-free aggregate updates

### 💾 **Intelligent Caching**
- **Multi-Level Cache**: Thumbnails and optimized cache images
- **Configurable Quality**: Auto-adjust based on source image analysis
- **Cache Folders**: Organized by priority and size limits
- **Atomic Updates**: Thread-safe concurrent operations
- **Statistics Tracking**: Real-time cache folder metrics

### 🔐 **Authentication & Security**
- **JWT Authentication**: Secure token-based auth
- **Role-Based Access**: Admin, LibraryManager, User roles
- **Session Management**: Secure session handling
- **Password Security**: Bcrypt hashing with salt

---

## 🏗️ Architecture

### **System Overview**

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│  React Frontend │─────▶│   .NET API       │─────▶│    MongoDB      │
│  (Vite + TS)    │      │  (REST + JWT)    │      │  (Document DB)  │
└─────────────────┘      └──────────────────┘      └─────────────────┘
                                  │
                                  │ Publishes Messages
                                  ▼
                         ┌──────────────────┐
                         │    RabbitMQ      │
                         │  (Message Broker)│
                         └──────────────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │                           │
                    ▼                           ▼
          ┌──────────────────┐        ┌──────────────────┐
          │ Worker Service   │        │   Scheduler      │
          │ (Consumers)      │        │   (Hangfire)     │
          │                  │        │                  │
          │ • LibraryScan    │        │ • Cron Jobs      │
          │ • CollectionScan │        │ • Job Binding    │
          │ • ImageProcess   │        │ • Auto-Sync      │
          │ • Thumbnail Gen  │        │                  │
          │ • Cache Gen      │        │                  │
          └──────────────────┘        └──────────────────┘
```

### **Clean Architecture Layers**

```
┌──────────────────────────────────────────────────────────┐
│                    Presentation Layer                     │
│  • ImageViewer.Api (REST API + Swagger)                  │
│  • React Frontend (UI Components)                         │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│                   Application Layer                       │
│  • ImageViewer.Application (Services, DTOs, Mappings)    │
│  • Use Cases, Business Logic, Validation                 │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│                     Domain Layer                          │
│  • ImageViewer.Domain (Entities, Interfaces, Events)     │
│  • Domain Models, Business Rules, Value Objects          │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│                 Infrastructure Layer                      │
│  • ImageViewer.Infrastructure (Repositories, Services)   │
│  • MongoDB, RabbitMQ, File System, External Services     │
└──────────────────────────────────────────────────────────┘
```

### **Message Flow**

```
Library Scan Triggered
  ↓
API creates ScheduledJob (orphaned initially)
  ↓
Scheduler syncs every 5 min
  ↓
Hangfire recurring job created → HangfireJobId set
  ↓
Cron fires → LibraryScanJobHandler
  ↓
Publish LibraryScanMessage → RabbitMQ
  ↓
Worker.LibraryScanConsumer
  ↓
BulkService discovers collections (folders + archives)
  ↓
Create Collection entities → Publish CollectionScanMessage (×N)
  ↓
Worker.CollectionScanConsumer
  ↓
Scan media files → Publish ImageProcessingMessage (×M)
  ↓
Worker.ImageProcessingConsumer
  ↓
Create embedded images → Publish ThumbnailGeneration + CacheGeneration
  ↓
Worker.ThumbnailGenerationConsumer + CacheGenerationConsumer
  ↓
Generate files → Update cache folder statistics (atomic)
  ↓
Complete! Library statistics updated in real-time
```

---

## 🚀 Quick Start

### **Prerequisites**

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [MongoDB 7.0+](https://www.mongodb.com/try/download/community)
- [RabbitMQ 3.12+](https://www.rabbitmq.com/download.html)
- [Node.js 18+](https://nodejs.org/) (for frontend)

### **Local Development (Windows)**

```powershell
# 1. Clone repository
git clone https://github.com/yourusername/image-viewer.git
cd image-viewer

# 2. Start dependencies (Docker)
docker-compose up -d mongodb rabbitmq

# 3. Start all services
.\start-all-services.bat

# Services will start:
# - API: https://localhost:11001
# - Worker: Processing in background
# - Scheduler: Hangfire jobs
# - Frontend: http://localhost:3000
```

### **Docker Deployment**

```bash
# Build and start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### **Manual Service Startup**

```powershell
# Start API
cd src/ImageViewer.Api
dotnet run

# Start Worker (new terminal)
cd src/ImageViewer.Worker
dotnet run

# Start Scheduler (new terminal)
cd src/ImageViewer.Scheduler
dotnet run

# Start Frontend (new terminal)
cd client
npm install
npm run dev
```

---

## 📖 Usage Guide

### **1. Create a Library**

```bash
POST /api/v1/libraries
{
  "name": "My Photos",
  "path": "D:\\Photos",
  "ownerId": "user-id",
  "autoScan": true  # Enable automatic scheduled scanning
}
```

### **2. Trigger Manual Scan**

```bash
POST /api/v1/libraries/{libraryId}/scan
```

**What Happens:**
1. Library scanned for folders and archives
2. Collections auto-created for discovered items
3. Images extracted and metadata generated
4. Thumbnails and cache generated in background
5. Library statistics updated in real-time

### **3. Browse Collections**

- **Collections List**: `/collections`
- **Collection Detail**: `/collections/{id}`
- **Image Viewer**: `/viewer/{collectionId}?imageId={firstImageId}`
- **Random Collection**: Click Shuffle icon or press `Ctrl+R`

### **4. Image Viewer Features**

#### **View Modes**
- Press `1`: Single image
- Press `2`: Double (side-by-side)
- Press `3`: Triple
- Press `4`: Quad (2×2 grid)

#### **Navigation**
- `←` `→`: Previous/Next
- `Space`: Toggle slideshow
- `Ctrl+R`: Random collection
- `Link` icon: Toggle cross-collection navigation

#### **Zoom & Transform**
- `+` `-`: Zoom in/out
- `Ctrl+Wheel`: Zoom with mouse (scroll mode)
- `R`: Rotate 90°
- `0`: Reset zoom

#### **Modes**
- `Scroll/Page` toggle: Continuous scroll or paging
- `Cross-Collection`: Navigate across collections seamlessly

---

## 🔧 Configuration

### **Environment Variables**

Create `.env` files based on environment:

```bash
# Database
MONGODB_CONNECTION_STRING=mongodb://localhost:27017
MONGODB_DATABASE_NAME=imageviewer

# RabbitMQ
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=admin
RABBITMQ_PASSWORD=admin123

# JWT
JWT_SECRET_KEY=your-super-secret-key-min-32-chars
JWT_ISSUER=ImageViewer
JWT_AUDIENCE=ImageViewer.Users
JWT_EXPIRATION_MINUTES=60

# Hangfire (Scheduler)
HANGFIRE_JOB_SYNC_INTERVAL=5  # Minutes

# Cache Settings
CACHE_FOLDER_PATH=D:\ImageViewerCache
THUMBNAIL_WIDTH=200
THUMBNAIL_HEIGHT=200
CACHE_WIDTH=1920
CACHE_HEIGHT=1080
CACHE_QUALITY=85
```

### **Hangfire Scheduler**

Configure cron expressions for library auto-scan:

```json
{
  "cronExpression": "0 2 * * *"  // Daily at 2 AM
}
```

Common patterns:
- `*/30 * * * *` - Every 30 minutes
- `0 */4 * * *` - Every 4 hours
- `0 0 * * 0` - Weekly on Sunday at midnight

### **appsettings.json**

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "imageviewer"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "admin",
    "Password": "admin123"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## 📊 Performance

### **Scalability**

Tested with:
- **25,000 collections**
- **2,000,000+ media files**
- **Concurrent processing**: 100+ parallel operations
- **Log optimization**: 99.35% reduction (500GB → 3GB)

### **Optimizations**

1. **Atomic Database Operations**
   - MongoDB `$inc` for statistics
   - Aggregation pipelines for complex updates
   - No race conditions in concurrent bulk operations

2. **Efficient Caching**
   - Single-transaction size + count updates
   - Atomic collection ID tracking
   - Prevents inconsistency on crashes

3. **Smart Logging**
   - Per-file logs at DEBUG level
   - Progress indicators every 50 files
   - Production-ready log volumes

4. **Message Queue**
   - Exact-match routing (no wildcard overhead)
   - Batch processing capabilities
   - Dead letter queue for failed messages

---

## 🧪 Testing

### **Run Tests**

```bash
# All tests
dotnet test

# Specific project
dotnet test src/ImageViewer.Test

# With coverage
dotnet test /p:CollectCoverage=true
```

### **Test Categories**

- **Unit Tests**: Domain logic, services
- **Integration Tests**: Database, message queue
- **Performance Tests**: Bulk operations, concurrent processing
- **Contract Tests**: API endpoint validation

---

## 📚 API Documentation

### **Base URL**
```
Development: https://localhost:11001/api/v1
Production: https://your-domain.com/api/v1
```

### **Swagger UI**
```
https://localhost:11001/swagger
```

### **Key Endpoints**

#### **Authentication**
```http
POST /auth/login
POST /auth/register
POST /auth/refresh-token
POST /auth/enable-2fa
```

#### **Libraries**
```http
GET    /libraries
POST   /libraries
GET    /libraries/{id}
DELETE /libraries/{id}
POST   /libraries/{id}/scan                    # Manual scan
POST   /libraries/{id}/recreate-job            # Recreate Hangfire job
GET    /libraries/orphaned-jobs                # Find orphaned jobs
DELETE /libraries/orphaned-jobs/{jobId}        # Remove orphaned job
```

#### **Collections**
```http
GET    /collections
POST   /collections
GET    /collections/{id}
GET    /collections/{id}/navigation             # Prev/next collection IDs
GET    /collections/{id}/siblings               # Related collections
DELETE /collections/{id}
```

#### **Images**
```http
GET    /images?collectionId={id}
GET    /images/{collectionId}/{imageId}/file
GET    /images/{collectionId}/{imageId}/thumbnail
GET    /images/{collectionId}/{imageId}/cache
```

#### **Random**
```http
GET    /random                                  # Get random collection
```

#### **Scheduled Jobs**
```http
GET    /scheduledjobs
GET    /scheduledjobs/{id}
PUT    /scheduledjobs/{id}/cron                # Update cron expression
POST   /scheduledjobs/{id}/enable
POST   /scheduledjobs/{id}/disable
DELETE /scheduledjobs/{id}
```

---

## 🗄️ Database Schema

### **MongoDB Collections**

- `libraries` - Library entities with settings and statistics
- `collections` - Collection aggregates with embedded images
- `scheduled_jobs` - Hangfire job definitions with LibraryId binding
- `scheduled_job_runs` - Job execution history
- `users` - User accounts and authentication
- `cache_folders` - Cache folder management with atomic statistics

### **Key Design Patterns**

- **Embedded Documents**: Images, thumbnails, cache within collections
- **Aggregates**: Collection as aggregate root
- **Atomic Operations**: `$inc`, `$addToSet`, `$size` for concurrency
- **Aggregation Pipelines**: Race-condition-free count updates

---

## 🔄 Background Jobs

### **Job Types**

1. **LibraryScan**
   - Discovers collections in library path
   - Creates Collection entities
   - Updates library statistics
   - Scheduled via cron expression

2. **CollectionScan**
   - Scans collection for media files
   - Extracts from archives
   - Publishes ImageProcessing messages
   - Updates library statistics (media count, size)

3. **ImageProcessing**
   - Creates embedded image records
   - Extracts metadata (dimensions, format, size)
   - Publishes Thumbnail + Cache generation

4. **ThumbnailGeneration**
   - Generates preview thumbnails
   - Updates cache folder statistics (atomic)
   - Saved to cache folder

5. **CacheGeneration**
   - Generates optimized cache images
   - Auto-adjusts quality based on source
   - Updates cache folder statistics (atomic)

### **Job Monitoring**

- **Orphaned Job Detection**: Jobs without Hangfire binding
- **Job Recreation**: Force re-binding to Hangfire
- **Statistics**: Run count, success rate, errors
- **UI Integration**: Real-time job status in library screen

---

## 🚀 Deployment

### **Production Deployment**

```bash
# Docker Compose (Recommended)
docker-compose -f docker-compose.yml -f docker-compose.windows.yml up -d

# Or use deployment scripts
.\deploy-local.ps1          # Local deployment
.\deploy-docker.ps1         # Docker deployment
.\deploy-silent.ps1         # Silent background deployment
```

### **Service Management**

```powershell
# Start services
.\start-all-services.bat

# Stop services
.\stop-all-services.bat

# Check status
.\status-services.ps1

# View logs
.\view-logs.ps1
```

### **Health Checks**

```bash
# API Health
GET https://localhost:11001/health

# Expected Response
{
  "status": "Healthy",
  "checks": {
    "mongodb": "Healthy",
    "rabbitmq": "Healthy"
  }
}
```

---

## 🔍 Troubleshooting

### **Common Issues**

#### **1. Orphaned Scheduled Jobs**

**Symptom**: Job created but `hangfireJobId` is null

**Solution**:
```bash
# Option 1: Wait 5 minutes for auto-sync
# Scheduler syncs jobs every 5 minutes

# Option 2: Force recreation
POST /api/v1/libraries/{libraryId}/recreate-job

# Option 3: Delete and recreate library
DELETE /api/v1/libraries/orphaned-jobs/{jobId}
```

#### **2. Collections Not Scanned**

**Symptom**: Collections created but no images

**Check Worker Logs**:
```powershell
Get-Content src\ImageViewer.Worker\logs\*.log | Select-String "CollectionScan"
```

**Common Causes**:
- RabbitMQ queue routing mismatch → Fixed in latest version
- Worker not running → Start with `.\start-all-services.bat`
- Archive extraction errors → Check archive format support

#### **3. RabbitMQ Queue Issues**

**Clear and recreate queues**:
```powershell
.\clear-rabbitmq-queues.ps1
# Then restart Worker to recreate with correct bindings
```

#### **4. Frontend 401 Unauthorized**

**Check**:
1. Token in localStorage: `auth_token`
2. Vite proxy: `https://localhost:11001` (not http!)
3. API HTTPS redirect removed: `app.UseHttpsRedirection()` commented out
4. Axios interceptor: Automatically adds `Authorization: Bearer` header

---

## 🛠️ Development

### **Project Structure**

```
src/
├── ImageViewer.Api/              # REST API (ASP.NET Core)
│   ├── Controllers/              # API endpoints
│   ├── Middleware/               # Custom middleware
│   └── Program.cs                # API startup
│
├── ImageViewer.Application/      # Business Logic
│   ├── Services/                 # Application services
│   ├── DTOs/                     # Data transfer objects
│   ├── Mappings/                 # Entity ↔ DTO mappings
│   └── Interfaces/               # Service contracts
│
├── ImageViewer.Domain/           # Core Domain
│   ├── Entities/                 # Domain entities
│   ├── ValueObjects/             # Value objects
│   ├── Events/                   # Domain events
│   ├── Enums/                    # Enumerations
│   └── Interfaces/               # Repository contracts
│
├── ImageViewer.Infrastructure/   # Infrastructure
│   ├── Data/                     # MongoDB repositories
│   ├── Services/                 # External services (RabbitMQ, etc.)
│   ├── Messaging/                # Message definitions
│   └── Extensions/               # DI extensions
│
├── ImageViewer.Worker/           # Background Worker
│   └── Services/                 # RabbitMQ consumers
│       ├── LibraryScanConsumer
│       ├── CollectionScanConsumer
│       ├── ImageProcessingConsumer
│       ├── ThumbnailGenerationConsumer
│       └── CacheGenerationConsumer
│
├── ImageViewer.Scheduler/        # Hangfire Scheduler
│   ├── Jobs/                     # Job handlers
│   │   └── LibraryScanJobHandler
│   ├── Services/                 # Scheduler services
│   └── SchedulerWorker.cs        # Background sync worker
│
└── ImageViewer.Test/             # Tests
    ├── Unit/                     # Unit tests
    ├── Integration/              # Integration tests
    └── Performance/              # Performance tests

client/
├── src/
│   ├── components/               # React components
│   ├── pages/                    # Page components
│   ├── services/                 # API clients
│   ├── hooks/                    # Custom React hooks
│   ├── contexts/                 # React contexts
│   └── utils/                    # Utilities
└── public/                       # Static assets
```

### **Coding Standards**

#### **C# (.NET)**
- PascalCase: Classes, Methods, Properties
- camelCase: Variables, Parameters
- Prefix: Interfaces with `I`, Private fields with `_`
- Async methods end with `Async`
- Use `ObjectId` for MongoDB IDs
- Comments in English, Chinese, Vietnamese

#### **TypeScript (React)**
- PascalCase: Components, Interfaces
- camelCase: Functions, variables
- Strong typing: No `any` types
- Functional components with hooks
- React Query for data fetching

---

## 📈 Monitoring

### **Hangfire Dashboard**

Integrated into Library screen:
- Job status (Active/Paused/Orphaned)
- Cron expression editor
- Execution statistics
- Next run time
- Success/failure rates

### **Logs**

```
src/ImageViewer.Api/logs/        # API logs
src/ImageViewer.Worker/logs/     # Worker logs
src/ImageViewer.Scheduler/logs/  # Scheduler logs
```

**Log Levels**:
- **Production**: Information (per-collection summaries + progress every 50 files)
- **Development**: Debug (per-file details)
- **Errors**: Always logged with full stack traces

---

## 🤝 Contributing

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### **Commit Message Convention**

```
feat: Add new feature
fix: Bug fix
perf: Performance improvement
docs: Documentation
refactor: Code refactoring
test: Add tests
chore: Maintenance
```

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **SkiaSharp**: High-performance image processing
- **Hangfire**: Background job scheduling
- **RabbitMQ**: Message queue
- **MongoDB**: Document database
- **React Query**: Data fetching and caching
- **Tailwind CSS**: Utility-first CSS

---

## 📞 Support

For issues, questions, or feature requests:
- 📧 Email: support@imageviewer.com
- 🐛 Issues: [GitHub Issues](https://github.com/yourusername/image-viewer/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/yourusername/image-viewer/discussions)

---

## 🗺️ Roadmap

- [ ] AI-powered image tagging
- [ ] Facial recognition and grouping
- [ ] Video playback support
- [ ] Mobile app (React Native)
- [ ] Cloud storage integration (S3, Azure Blob)
- [ ] Social features (sharing, comments)
- [ ] Advanced search filters
- [ ] Batch editing tools

---

**⭐ Star this repo if you find it useful!**

Made with ❤️ by the ImageViewer team

