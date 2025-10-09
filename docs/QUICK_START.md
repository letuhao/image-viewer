# Quick Start Guide

## 📁 Finding What You Need

### Development
```bash
# Run tests
./scripts/development/run-test.ps1

# Setup development environment
./scripts/development/setup-aiasag.ps1

# Manual operations
./scripts/development/manual-thumbnail-trigger.ps1
./scripts/development/queue-all-thumbnails.ps1
```

### Deployment
```bash
# Start services
./scripts/deployment/start-api.ps1

# Stop services
./scripts/deployment/stop-api.ps1
./scripts/deployment/stop-services.ps1

# Deploy
./scripts/deployment/deploy-local.ps1
./scripts/deployment/deploy-quick.ps1
./scripts/deployment/deploy-docker.ps1
```

### Maintenance
```bash
# Clean database
./scripts/maintenance/clear-database.ps1

# Clean RabbitMQ queues
./scripts/maintenance/clear-rabbitmq-queues.ps1

# Check system
./scripts/maintenance/check-dependencies.ps1
./scripts/maintenance/check-services.ps1
```

## 🐳 Docker

### Using Docker Compose
```bash
# Main compose file (in root for convenience)
docker-compose up -d

# Windows-specific
docker-compose -f deployment/docker-compose/docker-compose.windows.yml up -d

# With overrides
docker-compose -f docker-compose.yml -f deployment/docker-compose/docker-compose.override.yml up -d
```

### Building Images
```bash
# API
docker build -f deployment/docker/Dockerfile -t imageviewer-api .

# Worker
docker build -f deployment/docker/Dockerfile.Worker -t imageviewer-worker .
```

## ⚙️ Configuration

All configuration files are in `config/`:

```bash
config/
├── env.development      # Development environment
├── env.production       # Production environment
├── env.staging          # Staging environment
├── env.example          # Template for new environments
├── appsettings.Local.json  # Local API settings
└── bulk-test.json       # Bulk operation test config
```

### Setting Up Environment
```bash
# Copy example and customize
cp config/env.example config/.env
```

## 📚 Documentation

Documentation is organized by category in `docs/`:

- **01-requirements/** - Feature requirements and analysis
- **02-architecture/** - System design and architecture
- **03-api/** - API documentation
- **04-database/** - Database schema and design
- **05-deployment/** - Deployment guides
- **07-migration/** - Migration plans and histories
- **08-source-code-review/** - Code review reports
- **09-troubleshooting/** - Bug fixes and solutions

### Key Documents
- [Architecture Overview](docs/02-architecture/ARCHITECTURE_DESIGN.md)
- [API Specification](docs/03-api/API_SPECIFICATION.md)
- [Database Schema](docs/04-database/DATABASE_SCHEMA.md)
- [Deployment Guide](docs/05-deployment/DEPLOY_README.md)
- [Windows Deployment](docs/05-deployment/README-Windows-Deployment.md)
- [Troubleshooting](docs/09-troubleshooting/BUGS_FOUND_AND_FIXED.md)

## 🏗️ Building

### .NET Solution
```bash
# Build all projects
dotnet build src/ImageViewer.sln

# Build specific project
dotnet build src/ImageViewer.Api/ImageViewer.Api.csproj
dotnet build src/ImageViewer.Worker/ImageViewer.Worker.csproj

# Run tests
dotnet test src/ImageViewer.Test/ImageViewer.Test.csproj
```

### Development Mode
```bash
# Run API
cd src/ImageViewer.Api
dotnet run

# Run Worker
cd src/ImageViewer.Worker
dotnet run
```

## 🔧 Common Tasks

### Add a New Collection
```bash
# Using bulk add script
./scripts/development/test-bulk-fix.ps1

# Or use API
curl -X POST http://localhost:11000/api/v1/bulk/collections \
  -H "Content-Type: application/json" \
  -d '{"parentPath": "L:\\test", "autoScan": true}'
```

### Monitor Background Jobs
```bash
# View jobs in MongoDB
mongosh mongodb://localhost:27017/image_viewer
> db.background_jobs.find().pretty()

# Or use monitoring script
./scripts/development/test-enhanced-job-monitoring.ps1
```

### Clear All Data (Development)
```bash
# Clear database
./scripts/maintenance/clear-database.ps1

# Clear message queues
./scripts/maintenance/clear-rabbitmq-queues.ps1
```

## 🚀 First Time Setup

1. **Install Dependencies**
   ```bash
   ./scripts/maintenance/check-dependencies.ps1
   ```

2. **Configure Environment**
   ```bash
   cp config/env.example config/.env
   # Edit config/.env with your settings
   ```

3. **Start Infrastructure**
   ```bash
   docker-compose up -d mongodb rabbitmq
   ```

4. **Run Migrations** (if needed)
   ```bash
   # Check migration docs
   cat docs/07-migration/MONGODB_MIGRATION.md
   ```

5. **Start Services**
   ```bash
   ./scripts/deployment/start-api.ps1
   ```

6. **Verify**
   ```bash
   ./scripts/maintenance/check-services.ps1
   ```

## 📖 Next Steps

- Read [Architecture Design](docs/02-architecture/ARCHITECTURE_DESIGN.md) for system overview
- Check [API Documentation](docs/03-api/API_SPECIFICATION.md) for available endpoints
- Review [Deployment Guide](docs/05-deployment/DEPLOY_README.md) for production setup
- Browse [Troubleshooting](docs/09-troubleshooting/) for common issues

## 🆘 Getting Help

- Check `docs/09-troubleshooting/` for known issues
- Review `docs/08-source-code-review/` for code insights
- See `docs/ROOT_FOLDER_ORGANIZATION_PLAN.md` for structure details

