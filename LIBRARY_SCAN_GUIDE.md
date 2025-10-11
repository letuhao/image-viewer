# Library Scan Guide

## Overview

This guide explains how library scanning works and how to monitor the process from library creation to collection generation.

## How It Works

### Library Creation Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. USER CREATES LIBRARY (Frontend)                          │
│    - Name: "E-Media"                                         │
│    - Path: "L:\test"                                         │
│    - AutoScan: true                                          │
├─────────────────────────────────────────────────────────────┤
│ 2. LIBRARY CREATED IN DATABASE                               │
│    - Library entity stored in MongoDB                        │
│    - ScheduledJob entity created if AutoScan=true            │
│    - Status: Library exists but NO collections yet           │
├─────────────────────────────────────────────────────────────┤
│ 3A. AUTOMATIC SCAN (Scheduled)                               │
│    - Scheduler syncs jobs every 5 minutes                    │
│    - Hangfire picks up new recurring job                     │
│    - Job runs at scheduled time (default: 2 AM daily)        │
├─────────────────────────────────────────────────────────────┤
│ 3B. MANUAL SCAN (Immediate) ⚡                               │
│    - User clicks "Scan Now" button                          │
│    - API publishes LibraryScanMessage to RabbitMQ            │
│    - Worker processes immediately                            │
├─────────────────────────────────────────────────────────────┤
│ 4. WORKER SCANS LIBRARY                                      │
│    - LibraryScanConsumer receives message                    │
│    - Scans file system at library path                       │
│    - Identifies collection folders (folders with images)     │
│    - Creates Collection entities in database                 │
│    - Publishes CollectionScanMessage for each collection     │
├─────────────────────────────────────────────────────────────┤
│ 5. COLLECTIONS CREATED ✅                                    │
│    - Collection entities stored in MongoDB                   │
│    - Visible in frontend Collections page                    │
│    - Linked to parent Library                                │
└─────────────────────────────────────────────────────────────┘
```

## Quick Start - Manual Scan

### Step 1: Create Library

1. Navigate to **Libraries** page
2. Click **Add Library** button
3. Fill in form:
   - Name: `E-Media`
   - Path: `L:\test` (must exist on server)
   - AutoScan: ✅ Enabled
4. Click **Create Library**

### Step 2: Trigger Scan Immediately

1. Find your library in the list
2. Click the **🔄 Scan Now** button (refresh icon)
3. Toast message: "Scan triggered for E-Media"

### Step 3: Monitor Progress

**Option A: Frontend (Real-time)**
- Stay on Libraries page
- Scheduler section auto-refreshes every 30s
- Watch for:
  - Job runs count increasing
  - Last run status
  - Collection count increasing

**Option B: MongoDB (Direct)**
Run monitoring script:
```powershell
.\monitor-library-scan.ps1 -LibraryId "68ea86ef6912ca662238eb5c"
```

**Option C: Check Collections Page**
- Navigate to **Collections** page
- New collections should appear
- Linked to your library

## Monitoring Tools

### 1. Frontend UI

**Libraries Page**:
- Library statistics (Collections, Media Items, Size)
- Scheduler job status (Active/Inactive)
- Job execution history (when "Show Details" clicked)
- Last run time, status, duration
- Next scheduled run
- Success/failure counts

**Collections Page**:
- Lists all collections
- Filter by library (coming soon)
- Shows image counts, thumbnails

### 2. PowerShell Scripts

**monitor-library-scan.ps1**:
```powershell
# Monitor specific library
.\monitor-library-scan.ps1 -LibraryId "YOUR_LIBRARY_ID"

# Custom refresh interval
.\monitor-library-scan.ps1 -LibraryId "YOUR_ID" -RefreshInterval 3
```

Shows:
- ✅ Library info (name, path, autoScan setting)
- ✅ Scheduled job (cron, isEnabled, run counts)
- ✅ Recent job executions (last 3 runs)
- ✅ Collections created (name, path, image count)
- ✅ Active background jobs in Worker

### 3. API Endpoints

**Library Operations**:
```bash
# Get all libraries
GET /api/v1/libraries

# Get specific library
GET /api/v1/libraries/{id}

# Trigger manual scan
POST /api/v1/libraries/{id}/scan

# Update library settings
PUT /api/v1/libraries/{id}/settings
```

**Scheduled Jobs**:
```bash
# Get all scheduled jobs
GET /api/v1/scheduledjobs

# Get job for specific library
GET /api/v1/scheduledjobs/library/{libraryId}

# Get job execution history
GET /api/v1/scheduledjobs/{id}/runs

# Enable/disable job
POST /api/v1/scheduledjobs/{id}/enable
POST /api/v1/scheduledjobs/{id}/disable
```

**Collections**:
```bash
# Get all collections
GET /api/v1/collections

# Get collections for library (filter in query)
GET /api/v1/collections?libraryId={id}
```

## Architecture

### Components

1. **Frontend** (React + TypeScript)
   - Libraries page with management UI
   - Scan Now button for immediate triggering
   - Real-time job status display

2. **API** (ASP.NET Core)
   - LibrariesController - CRUD + Trigger Scan
   - ScheduledJobsController - Job monitoring
   - CollectionsController - Collection queries

3. **Scheduler** (Worker Service + Hangfire)
   - Manages recurring jobs
   - Syncs database jobs every 5 minutes
   - Publishes scan messages to RabbitMQ
   - Job execution tracking

4. **Worker** (Worker Service + RabbitMQ)
   - LibraryScanConsumer - Scans file system
   - Creates Collection entities
   - Triggers downstream processing

5. **Database** (MongoDB)
   - libraries collection
   - scheduled_jobs collection
   - scheduled_job_runs collection
   - collections collection

6. **Message Queue** (RabbitMQ)
   - library_scan_queue
   - collection.scan queue
   - Other processing queues

## Configuration

### Scheduler Sync Interval

File: `src/ImageViewer.Scheduler/appsettings.json`

```json
{
  "Hangfire": {
    "JobSynchronizationInterval": 5  // Minutes between database sync
  }
}
```

**Change to 1 minute for faster pickup**:
```json
"JobSynchronizationInterval": 1
```

### Scheduled Scan Time

Default: Daily at 2:00 AM

To change, update the cron expression when creating the job:
```csharp
// In ScheduledJobManagementService.CreateOrUpdateLibraryScanJobAsync
var cronExpression = "0 2 * * *";  // Change this
```

Common patterns:
- `0 2 * * *` - Daily at 2 AM
- `0 */6 * * *` - Every 6 hours
- `0 0 * * 0` - Weekly on Sunday at midnight
- `*/30 * * * *` - Every 30 minutes

## Troubleshooting

### Library Created But No Collections

**Possible Causes**:
1. **Path doesn't exist** on server
   - Solution: Verify path exists: `Test-Path "L:\test"`

2. **No image folders** in path
   - Solution: Ensure path contains folders with image files

3. **Scan not triggered yet**
   - Solution: Click "Scan Now" button or wait for scheduled time

4. **Worker not running**
   - Solution: Check with `.\status-services.ps1`

5. **RabbitMQ connection failed**
   - Solution: Check Worker logs for RabbitMQ errors

### Job Not Appearing in Scheduler

**Causes**:
1. **AutoScan disabled** during library creation
   - Solution: Enable via settings or recreate library

2. **Scheduler not synced yet** (5 min interval)
   - Solution: Wait or restart Scheduler to force sync

3. **Scheduler not running**
   - Solution: Check with `.\status-services.ps1`

### Collections Created But Empty

This means collections were created but image scanning hasn't completed yet.

**Solution**: Wait for CollectionScanConsumer to process, or check Worker logs.

## Best Practices

1. **Use Manual Scan for Testing**
   - Click "Scan Now" for immediate results
   - Don't wait for scheduled execution

2. **Monitor During Development**
   - Keep `monitor-library-scan.ps1` running
   - Watch logs in real-time
   - Use `.\view-logs.ps1` for detailed logs

3. **Check All Services Running**
   ```powershell
   .\status-services.ps1
   ```
   Should show:
   - ✅ API (port 11001)
   - ✅ Scheduler (Hangfire)
   - ✅ Worker (RabbitMQ consumers)
   - ✅ Frontend (port 3000)

4. **Use Collections Page**
   - After scan completes, check Collections page
   - New collections should appear
   - Click to view images

## Summary

Collections are **NOT** created immediately when you create a library because:
- Library creation only stores metadata
- Actual file scanning is asynchronous
- Worker scans file system and creates collections
- This keeps UI responsive and handles large libraries

Use **Scan Now button** for immediate scanning without waiting for scheduled time! ⚡

