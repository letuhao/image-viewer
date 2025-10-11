// MongoDB Index Creation Script for ImageViewer
// Run this after initial database setup or data migration
// Usage: mongosh < create-mongodb-indexes.js

use image_viewer;

print("\n=== Creating MongoDB Indexes for ImageViewer ===\n");

// ============================================================================
// COLLECTIONS - Most critical for performance (main UI screen)
// ============================================================================
print("Creating indexes for 'collections' collection...");

// 1. Library queries - filter by libraryId (used in library detail screen)
db.collections.createIndex(
    { "libraryId": 1, "isDeleted": 1 },
    { name: "idx_libraryId_isDeleted", background: true }
);
print("✓ Created index: libraryId + isDeleted");

// 2. Path lookup - unique constraint, used for detecting duplicates
db.collections.createIndex(
    { "path": 1, "isDeleted": 1 },
    { name: "idx_path_isDeleted", unique: true, background: true }
);
print("✓ Created index: path + isDeleted (unique)");

// 3. Active collections filter - commonly used
db.collections.createIndex(
    { "isActive": 1, "isDeleted": 1 },
    { name: "idx_isActive_isDeleted", background: true }
);
print("✓ Created index: isActive + isDeleted");

// 4. Collection type filter - used in type-based queries
db.collections.createIndex(
    { "type": 1, "isDeleted": 1 },
    { name: "idx_type_isDeleted", background: true }
);
print("✓ Created index: type + isDeleted");

// 5. Text search index - for searching by name, description, tags
db.collections.createIndex(
    {
        "name": "text",
        "description": "text",
        "metadata.tags": "text",
        "searchIndex.keywords": "text"
    },
    {
        name: "idx_text_search",
        background: true,
        weights: {
            "name": 10,                      // Name is most important
            "metadata.tags": 5,              // Tags are second
            "searchIndex.keywords": 3,       // Keywords third
            "description": 1                 // Description least important
        }
    }
);
print("✓ Created text index: name, description, tags, keywords");

// 6. Sort by creation date (for "newest" sorting in UI)
db.collections.createIndex(
    { "createdAt": -1, "isDeleted": 1 },
    { name: "idx_createdAt_desc_isDeleted", background: true }
);
print("✓ Created index: createdAt (descending) + isDeleted");

// 7. Sort by update date (for "recently updated")
db.collections.createIndex(
    { "updatedAt": -1, "isDeleted": 1 },
    { name: "idx_updatedAt_desc_isDeleted", background: true }
);
print("✓ Created index: updatedAt (descending) + isDeleted");

// 8. Embedded images path lookup (for finding which collection contains an image)
db.collections.createIndex(
    { "images.path": 1 },
    { name: "idx_images_path", background: true, sparse: true }
);
print("✓ Created index: images.path (sparse)");

// 9. Cache images path lookup (for cache invalidation)
db.collections.createIndex(
    { "cacheImages.cachePath": 1 },
    { name: "idx_cacheImages_cachePath", background: true, sparse: true }
);
print("✓ Created index: cacheImages.cachePath (sparse)");

// ============================================================================
// USERS - Authentication and authorization
// ============================================================================
print("\nCreating indexes for 'users' collection...");

// 1. Username lookup - used for login
db.users.createIndex(
    { "username": 1 },
    { name: "idx_username", unique: true, background: true }
);
print("✓ Created index: username (unique)");

// 2. Email lookup - used for login and password reset
db.users.createIndex(
    { "email": 1 },
    { name: "idx_email", unique: true, background: true }
);
print("✓ Created index: email (unique)");

// 3. Active users filter
db.users.createIndex(
    { "isActive": 1, "isDeleted": 1 },
    { name: "idx_isActive_isDeleted_users", background: true }
);
print("✓ Created index: isActive + isDeleted");

// 4. Role-based access control
db.users.createIndex(
    { "role": 1, "isActive": 1 },
    { name: "idx_role_isActive", background: true }
);
print("✓ Created index: role + isActive");

// ============================================================================
// LIBRARIES - Library management
// ============================================================================
print("\nCreating indexes for 'libraries' collection...");

// 1. Owner queries - get libraries by owner
db.libraries.createIndex(
    { "ownerId": 1, "isDeleted": 1 },
    { name: "idx_ownerId_isDeleted", background: true }
);
print("✓ Created index: ownerId + isDeleted");

// 2. Path lookup - unique constraint
db.libraries.createIndex(
    { "path": 1, "isDeleted": 1 },
    { name: "idx_path_isDeleted_libraries", unique: true, background: true }
);
print("✓ Created index: path + isDeleted (unique)");

// 3. Active libraries filter
db.libraries.createIndex(
    { "isActive": 1, "isDeleted": 1 },
    { name: "idx_isActive_isDeleted_libraries", background: true }
);
print("✓ Created index: isActive + isDeleted");

// 4. Public libraries (for public access)
db.libraries.createIndex(
    { "isPublic": 1, "isActive": 1, "isDeleted": 1 },
    { name: "idx_isPublic_isActive_isDeleted", background: true }
);
print("✓ Created index: isPublic + isActive + isDeleted");

// ============================================================================
// CACHE_FOLDERS - Cache management
// ============================================================================
print("\nCreating indexes for 'cache_folders' collection...");

// 1. Path lookup - unique constraint
db.cache_folders.createIndex(
    { "path": 1 },
    { name: "idx_path_cache_folders", unique: true, background: true }
);
print("✓ Created index: path (unique)");

// 2. Active cache folders ordered by priority (hot path!)
db.cache_folders.createIndex(
    { "isActive": 1, "priority": 1 },
    { name: "idx_isActive_priority", background: true }
);
print("✓ Created index: isActive + priority");

// 3. Find by cached collection (for lookup)
db.cache_folders.createIndex(
    { "cachedCollections": 1 },
    { name: "idx_cachedCollections", background: true, sparse: true }
);
print("✓ Created index: cachedCollections (sparse)");

// ============================================================================
// SCHEDULED_JOBS - Job management
// ============================================================================
print("\nCreating indexes for 'scheduled_jobs' collection...");

// 1. Job type and enabled status (for job execution)
db.scheduled_jobs.createIndex(
    { "jobType": 1, "isEnabled": 1 },
    { name: "idx_jobType_isEnabled", background: true }
);
print("✓ Created index: jobType + isEnabled");

// 2. Library jobs (for library-specific job queries)
db.scheduled_jobs.createIndex(
    { "libraryId": 1, "isEnabled": 1 },
    { name: "idx_libraryId_isEnabled", background: true, sparse: true }
);
print("✓ Created index: libraryId + isEnabled (sparse)");

// 3. Next run time (for job scheduler)
db.scheduled_jobs.createIndex(
    { "nextRunAt": 1, "isEnabled": 1 },
    { name: "idx_nextRunAt_isEnabled", background: true, sparse: true }
);
print("✓ Created index: nextRunAt + isEnabled (sparse)");

// 4. Hangfire job ID lookup (for job synchronization)
db.scheduled_jobs.createIndex(
    { "hangfireJobId": 1 },
    { name: "idx_hangfireJobId", background: true, sparse: true }
);
print("✓ Created index: hangfireJobId (sparse)");

// ============================================================================
// BACKGROUND_JOBS - Background job tracking
// ============================================================================
print("\nCreating indexes for 'background_jobs' collection...");

// 1. Job status and type (for active job queries)
db.background_jobs.createIndex(
    { "status": 1, "jobType": 1 },
    { name: "idx_status_jobType", background: true }
);
print("✓ Created index: status + jobType");

// 2. Created date (for cleanup and history)
db.background_jobs.createIndex(
    { "createdAt": -1 },
    { name: "idx_createdAt_desc_background_jobs", background: true }
);
print("✓ Created index: createdAt (descending)");

// 3. Collection jobs (for collection-specific job queries)
db.background_jobs.createIndex(
    { "entityId": 1, "jobType": 1 },
    { name: "idx_entityId_jobType", background: true, sparse: true }
);
print("✓ Created index: entityId + jobType (sparse)");

// ============================================================================
// REFRESH_TOKENS - Token management
// ============================================================================
print("\nCreating indexes for 'refresh_tokens' collection...");

// 1. Token lookup (for authentication)
db.refresh_tokens.createIndex(
    { "token": 1 },
    { name: "idx_token", unique: true, background: true }
);
print("✓ Created index: token (unique)");

// 2. User tokens (for user session management)
db.refresh_tokens.createIndex(
    { "userId": 1, "expiresAt": 1 },
    { name: "idx_userId_expiresAt", background: true }
);
print("✓ Created index: userId + expiresAt");

// 3. Token expiration (for cleanup jobs)
db.refresh_tokens.createIndex(
    { "expiresAt": 1 },
    { name: "idx_expiresAt", background: true, expireAfterSeconds: 0 }
);
print("✓ Created TTL index: expiresAt (auto-delete expired tokens)");

// ============================================================================
// SYSTEM_SETTINGS - System configuration
// ============================================================================
print("\nCreating indexes for 'system_settings' collection...");

// 1. Setting key lookup (for configuration queries)
db.system_settings.createIndex(
    { "key": 1 },
    { name: "idx_key_system_settings", unique: true, background: true }
);
print("✓ Created index: key (unique)");

// ============================================================================
// Summary
// ============================================================================
print("\n=== Index Creation Complete ===\n");
print("Indexes created for optimal query performance:");
print("  ✓ Collections: 9 indexes (including text search)");
print("  ✓ Users: 4 indexes");
print("  ✓ Libraries: 4 indexes");
print("  ✓ Cache Folders: 3 indexes");
print("  ✓ Scheduled Jobs: 4 indexes");
print("  ✓ Background Jobs: 3 indexes");
print("  ✓ Refresh Tokens: 3 indexes (including TTL)");
print("  ✓ System Settings: 1 index");
print("\nTotal: 31 indexes");
print("\n⚠️  IMPORTANT: All indexes created with background:true to avoid blocking");
print("    Check index build progress: db.currentOp({ op: 'command', 'command.createIndexes': { $exists: true } })\n");

