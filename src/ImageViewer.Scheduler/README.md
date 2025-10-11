# ImageViewer.Scheduler

Hangfire-based scheduler worker for automated library scanning and maintenance tasks.

## Overview

The Scheduler is a standalone worker service that manages recurring and scheduled tasks for the ImageViewer platform. It uses Hangfire with MongoDB storage to provide robust, distributed job scheduling.

## Architecture

```
Scheduler Worker (Hangfire)
    ↓ Executes on schedule (cron/interval)
LibraryScanJobHandler
    ↓ Publishes to RabbitMQ
library_scan_queue
    ↓ Consumed by Worker
LibraryScanConsumer
    ↓ Scans filesystem
Creates/Updates Collections
```

## Features

- **Automatic Library Scanning**: Scans library directories on schedule
- **Cron-Based Scheduling**: Flexible scheduling using cron expressions
- **MongoDB Persistence**: Jobs stored in `image_viewer_hangfire` database
- **RabbitMQ Integration**: Publishes scan messages to message queue
- **Execution Tracking**: Full history of job runs in `scheduled_job_runs` collection
- **Auto-Recovery**: Loads all enabled jobs from database on startup
- **Scalable**: Can run multiple scheduler instances for high availability

## Configuration

### appsettings.json

```json
{
  "Hangfire": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "image_viewer_hangfire",
    "ServerName": "ImageViewer-Scheduler-1",
    "WorkerCount": 5,
    "Queues": ["default", "scheduler", "library-scan", "critical"]
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "LibraryScanQueue": "library_scan_queue"
  }
}
```

## Job Types

### LibraryScan
- **Purpose**: Scans library directory for new/updated collections
- **Schedule**: Default: Daily at 2 AM (`0 2 * * *`)
- **Parameters**: `LibraryId` (ObjectId)
- **Handler**: `LibraryScanJobHandler`
- **Output**: Publishes `LibraryScanMessage` to RabbitMQ

## Database Collections

### scheduled_jobs
```
{
  _id: ObjectId,
  name: "Library Scan - MyLibrary",
  jobType: "LibraryScan",
  scheduleType: "Cron",
  cronExpression: "0 2 * * *",
  isEnabled: true,
  parameters: { LibraryId: ObjectId(...) },
  lastRunAt: ISODate,
  nextRunAt: ISODate,
  runCount: 42,
  successCount: 40,
  failureCount: 2
}
```

### scheduled_job_runs
```
{
  _id: ObjectId,
  scheduledJobId: ObjectId,
  scheduledJobName: "Library Scan - MyLibrary",
  status: "Completed",
  startedAt: ISODate,
  completedAt: ISODate,
  duration: 1234ms,
  result: { message: "Scan completed", ... }
}
```

## Running the Scheduler

### Development
```bash
cd src/ImageViewer.Scheduler
dotnet run
```

### Production (Docker)
```bash
docker-compose up -d imageviewer-scheduler
```

### Logs
- **Console**: Real-time log output
- **File**: `logs/scheduler-YYYYMMDD.log`
- **Retention**: 7 days
- **Size Limit**: 100MB per file

## API Integration

When a library is created/updated via the API:

1. **Library Created** (with AutoScan=true):
   - API creates `ScheduledJob` in MongoDB
   - Scheduler auto-loads on next startup (or restart)
   - Job executes on cron schedule

2. **Library Settings Updated**:
   - AutoScan enabled → Job enabled
   - AutoScan disabled → Job disabled

3. **Library Deleted**:
   - Associated `ScheduledJob` is deleted
   - Hangfire automatically removes recurring job

## Monitoring

### Hangfire Dashboard
- **URL**: `http://localhost:11001/hangfire` (via API server)
- **Access**: Admin/Scheduler role required (Production)
- **Features**: View jobs, retry failed jobs, trigger manual execution

### REST API
```
GET    /api/v1/scheduledjobs              # List all jobs
GET    /api/v1/scheduledjobs/{id}         # Get job details
GET    /api/v1/scheduledjobs/library/{id} # Get job for library
POST   /api/v1/scheduledjobs/{id}/enable  # Enable job
POST   /api/v1/scheduledjobs/{id}/disable # Disable job
DELETE /api/v1/scheduledjobs/{id}         # Delete job
GET    /api/v1/scheduledjobs/{id}/runs    # Execution history
GET    /api/v1/scheduledjobs/active       # Active jobs only
GET    /api/v1/scheduledjobs/runs/recent  # Recent executions
```

### Frontend UI
- **Libraries Page**: `/libraries`
- **Features**: View jobs, toggle AutoScan, view execution stats

## Troubleshooting

### Job Not Executing
1. Check `scheduled_jobs.isEnabled = true`
2. Verify `cronExpression` is valid
3. Check Scheduler logs for errors
4. Verify MongoDB connection
5. Check Hangfire dashboard for job status

### Job Fails Immediately
1. Check library path exists and is accessible
2. Verify RabbitMQ connection
3. Check Worker logs for message consumption
4. Review `scheduled_job_runs` for error details

### No Jobs Loaded
1. Restart Scheduler worker (auto-loads on startup)
2. Check MongoDB `scheduled_jobs` collection
3. Verify `isEnabled = true`
4. Check Scheduler startup logs

## Performance

- **Job Polling**: 15 seconds (configurable)
- **Worker Count**: 5 concurrent jobs (configurable)
- **Job Expiration**: 7 days (old job data auto-cleaned)
- **Max Execution Time**: 60 minutes per job (configurable)

## Dependencies

- **Hangfire.Core** 1.8.21
- **Hangfire.Mongo** 1.12.1
- **Hangfire.AspNetCore** 1.8.21
- **NCrontab** 3.4.0
- **Serilog** (logging)

## Future Enhancements

- [ ] Cache cleanup scheduled job
- [ ] Thumbnail cleanup scheduled job
- [ ] Stale job recovery scheduled job
- [ ] Custom cron expression per library
- [ ] Email notifications on job failure
- [ ] Slack/Discord webhook integration
- [ ] Job execution webhooks

