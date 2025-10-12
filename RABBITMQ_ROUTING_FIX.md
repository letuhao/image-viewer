# RabbitMQ Routing Key Fix

## Problem
There was an inconsistency in the routing keys for library scan messages:
- **Old routing key**: `library.scan`
- **New routing key**: `library_scan_queue`

This caused messages to be published but not consumed, leading to 119k+ messages in the DLQ.

## Changes Made

### 1. Standardized Routing Keys

All message types now use consistent routing keys:

| Message Type | Routing Key | Queue Name |
|-------------|-------------|------------|
| `CollectionScan` | `collection.scan` | `collection.scan` |
| `ThumbnailGeneration` | `thumbnail.generation` | `thumbnail.generation` |
| `CacheGeneration` | `cache.generation` | `cache.generation` |
| `CollectionCreation` | `collection.creation` | `collection.creation` |
| `BulkOperation` | `bulk.operation` | `bulk.operation` |
| `ImageProcessing` | `image.processing` | `image.processing` |
| **`LibraryScan`** | **`library_scan_queue`** | **`library_scan_queue`** |

### 2. Updated Files

#### Publishers:
- ✅ `LibrariesController.cs`: Uses `PublishAsync(scanMessage)` → auto-maps to `library_scan_queue`
- ✅ `LibraryScanJobHandler.cs`: Uses `PublishAsync(scanMessage, "library_scan_queue")`

#### Routing Configuration:
- ✅ `RabbitMQMessageQueueService.cs`: `GetDefaultRoutingKey()` → `library_scan_queue`
- ✅ `RabbitMQSetupService.cs`: Queue binding → `library_scan_queue`

#### Consumer:
- ✅ `LibraryScanConsumer.cs`: Listens on `library_scan_queue`

#### Recovery:
- ✅ `DlqRecoveryService.cs`: Maps `LibraryScan` → `library_scan_queue`

### 3. Message TTL Increase

Changed from **30 minutes** to **24 hours**:
- `src/ImageViewer.Api/appsettings.json`
- `src/ImageViewer.Worker/appsettings.json`

```json
"MessageTimeout": "24:00:00"
```

### 4. Automatic DLQ Recovery

Added `DlqRecoveryService` that runs on Worker startup:
- Reads all messages from `imageviewer.dlq`
- Extracts `MessageType` from headers
- Maps to correct routing key
- Republishes to original queue
- Logs recovery statistics

## Migration Steps

### If You Have Existing RabbitMQ with Old Bindings:

1. **Check current bindings:**
```bash
# Using RabbitMQ Management UI
http://localhost:15672/#/queues/%2F/library_scan_queue

# Or using rabbitmqctl
rabbitmqctl list_bindings | grep library_scan_queue
```

2. **Remove old binding (if exists):**
```bash
# If you see: imageviewer.exchange -> library_scan_queue [library.scan]
# You need to delete the queue and recreate it, OR:

# Unbind old routing key
rabbitmqadmin unbind source=imageviewer.exchange destination=library_scan_queue routing_key=library.scan

# Bind new routing key
rabbitmqadmin declare binding source=imageviewer.exchange destination=library_scan_queue routing_key=library_scan_queue
```

3. **Or simply delete and recreate queues:**
```powershell
# Stop all services
.\stop-api.ps1

# Delete RabbitMQ data (will recreate on next start)
# Windows: 
# C:\Users\<username>\AppData\Roaming\RabbitMQ\

# Or purge queues via Management UI
http://localhost:15672/#/queues
```

4. **Restart services:**
```powershell
.\start-api.ps1
```

The `RabbitMQSetupService` will automatically recreate all queues with correct bindings.

### Verify Recovery

After restarting the Worker, check logs for:

```
🔄 Starting DLQ Recovery Service...
⚠️  Found 119762 messages in DLQ. Starting recovery...
📦 Recovered 1000 messages so far...
================================
📊 DLQ RECOVERY SUMMARY
================================
✅ Total Recovered: 119762 messages

By Queue:
   collection.scan: 119762
================================
✅ DLQ is now empty!
```

## Testing

1. **Trigger a library scan:**
```bash
POST http://localhost:11000/api/v1/libraries/{libraryId}/scan
```

2. **Check Worker logs:**
```
Publishing message: Type=LibraryScan, ID=xxx, Exchange=imageviewer.exchange, RoutingKey=library_scan_queue, Queue=library_scan_queue
📚 Received library scan message: ...
```

3. **Verify no messages go to DLQ:**
```bash
# Check DLQ count (should be 0)
http://localhost:15672/#/queues/%2F/imageviewer.dlq
```

## Prevention

- **Message TTL**: Now 24 hours instead of 30 minutes
- **Auto Recovery**: Worker automatically recovers DLQ messages on startup
- **Consistent Routing**: All routing keys match queue names
- **Monitoring**: Check logs for "expired" messages in x-death headers

## Rollback

If you need to rollback:

1. Revert routing key changes in:
   - `RabbitMQMessageQueueService.cs`
   - `RabbitMQSetupService.cs`
   - `LibraryScanJobHandler.cs`

2. Change back to `library.scan`

3. Restart services

---

**Last Updated**: October 12, 2025
**Status**: ✅ Fixed and Tested

