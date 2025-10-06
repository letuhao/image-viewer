using MongoDB.Bson;

namespace ImageViewer.Test.Shared.Constants;

/// <summary>
/// Constants used across test projects
/// </summary>
public static class TestConstants
{
    // Test User IDs
    public static readonly ObjectId TestUserId = ObjectId.Parse("507f1f77bcf86cd799439011");
    public static readonly ObjectId TestAdminUserId = ObjectId.Parse("507f1f77bcf86cd799439012");
    public static readonly ObjectId TestLibraryId = ObjectId.Parse("507f1f77bcf86cd799439013");
    public static readonly ObjectId TestCollectionId = ObjectId.Parse("507f1f77bcf86cd799439014");
    public static readonly ObjectId TestMediaItemId = ObjectId.Parse("507f1f77bcf86cd799439015");

    // Test Data
    public const string TestUsername = "testuser";
    public const string TestAdminUsername = "admin";
    public const string TestEmail = "test@example.com";
    public const string TestAdminEmail = "admin@example.com";
    public const string TestPassword = "TestPassword123!";
    public const string TestPasswordHash = "hashed_test_password";
    
    // Test Collection Data
    public const string TestCollectionName = "Test Collection";
    public const string TestCollectionPath = "/test/collection";
    public const string TestCollectionDescription = "Test collection description";
    
    // Test Media Data
    public const string TestImageFilename = "test-image.jpg";
    public const string TestImagePath = "/test/collection/test-image.jpg";
    public const string TestImageFormat = "JPEG";
    
    // Test Network Data
    public const string TestIpAddress = "192.168.1.100";
    public const string TestUserAgent = "TestAgent/1.0";
    public const string TestLocation = "Test City, Test Country";
    
    // Test JWT Data
    public const string TestAccessToken = "test_access_token";
    public const string TestRefreshToken = "test_refresh_token";
    public const string TestSessionToken = "test_session_token";
    
    // Test File Paths
    public const string TestBasePath = "/test/base";
    public const string TestCachePath = "/test/cache";
    public const string TestTempPath = "/test/temp";
    
    // Test Database
    public const string TestDatabaseName = "imageviewer_test";
    public const string TestConnectionString = "mongodb://localhost:27017/imageviewer_test";
    
    // Test Performance
    public const int TestTimeoutMs = 5000;
    public const int TestRetryCount = 3;
    public const int TestBatchSize = 100;
    
    // Test Security
    public const string TestTwoFactorSecret = "TEST2FASECRET12345678901234567890";
    public const string TestBackupCode = "12345678";
    public const string TestTotpCode = "123456";
    
    // Test Notifications
    public const string TestNotificationTitle = "Test Notification";
    public const string TestNotificationMessage = "This is a test notification message";
    public const string TestNotificationType = "system";
    
    // Test Tags
    public const string TestTagName = "test-tag";
    public const string TestTagDescription = "Test tag description";
    public const string TestTagColor = "#FF0000";
    
    // Test Search
    public const string TestSearchQuery = "test search query";
    public const string TestSearchFilter = "test filter";
    
    // Test Analytics
    public const string TestEventType = "test_event";
    public const string TestEventData = "test_event_data";
    
    // Test Background Jobs
    public const string TestJobType = "test_job";
    public const string TestJobStatus = "pending";
    public const string TestJobData = "test_job_data";
}
