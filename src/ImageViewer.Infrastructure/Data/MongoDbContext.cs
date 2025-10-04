using MongoDB.Driver;
using ImageViewer.Domain.Entities;

namespace ImageViewer.Infrastructure.Data;

/// <summary>
/// MongoDB context for ImageViewer application
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    // Core Collections
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");
    public IMongoCollection<Library> Libraries => _database.GetCollection<Library>("libraries");
    public IMongoCollection<Collection> Collections => _database.GetCollection<Collection>("collections");
    public IMongoCollection<MediaItem> MediaItems => _database.GetCollection<MediaItem>("mediaItems");
    public IMongoCollection<ImageViewer.Domain.Entities.Tag> Tags => _database.GetCollection<ImageViewer.Domain.Entities.Tag>("tags");

    // System Collections
    public IMongoCollection<BackgroundJob> BackgroundJobs => _database.GetCollection<BackgroundJob>("backgroundJobs");
    public IMongoCollection<ViewSession> ViewSessions => _database.GetCollection<ViewSession>("viewSessions");
    public IMongoCollection<CacheFolder> CacheFolders => _database.GetCollection<CacheFolder>("cacheFolders");

    // Analytics Collections
    public IMongoCollection<UserBehaviorEvent> UserBehaviorEvents => _database.GetCollection<UserBehaviorEvent>("userBehaviorEvents");
    public IMongoCollection<UserAnalytics> UserAnalytics => _database.GetCollection<UserAnalytics>("userAnalytics");
    public IMongoCollection<ContentPopularity> ContentPopularity => _database.GetCollection<ContentPopularity>("contentPopularity");
    public IMongoCollection<SearchAnalytics> SearchAnalytics => _database.GetCollection<SearchAnalytics>("searchAnalytics");

    // Social Collections
    public IMongoCollection<UserCollection> UserCollections => _database.GetCollection<UserCollection>("userCollections");
    // public IMongoCollection<CollectionRating> CollectionRatings => _database.GetCollection<CollectionRating>("collectionRatings");
    public IMongoCollection<UserFollow> UserFollows => _database.GetCollection<UserFollow>("userFollows");
    public IMongoCollection<CollectionComment> CollectionComments => _database.GetCollection<CollectionComment>("collectionComments");
    public IMongoCollection<UserMessage> UserMessages => _database.GetCollection<UserMessage>("userMessages");
    // public IMongoCollection<Conversation> Conversations => _database.GetCollection<Conversation>("conversations");

    // Distribution Collections
    // public IMongoCollection<Torrent> Torrents => _database.GetCollection<Torrent>("torrents");
    // public IMongoCollection<DownloadLink> DownloadLinks => _database.GetCollection<DownloadLink>("downloadLinks");
    // public IMongoCollection<TorrentStatistics> TorrentStatistics => _database.GetCollection<TorrentStatistics>("torrentStatistics");
    // public IMongoCollection<LinkHealthChecker> LinkHealthCheckers => _database.GetCollection<LinkHealthChecker>("linkHealthCheckers");
    // public IMongoCollection<DownloadQualityOption> DownloadQualityOptions => _database.GetCollection<DownloadQualityOption>("downloadQualityOptions");
    // public IMongoCollection<DistributionNode> DistributionNodes => _database.GetCollection<DistributionNode>("distributionNodes");
    // public IMongoCollection<NodePerformanceMetrics> NodePerformanceMetrics => _database.GetCollection<NodePerformanceMetrics>("nodePerformanceMetrics");

    // Reward Collections
    public IMongoCollection<UserReward> UserRewards => _database.GetCollection<UserReward>("userRewards");
    public IMongoCollection<RewardTransaction> RewardTransactions => _database.GetCollection<RewardTransaction>("rewardTransactions");
    public IMongoCollection<RewardSetting> RewardSettings => _database.GetCollection<RewardSetting>("rewardSettings");
    public IMongoCollection<RewardAchievement> RewardAchievements => _database.GetCollection<RewardAchievement>("rewardAchievements");
    public IMongoCollection<RewardBadge> RewardBadges => _database.GetCollection<RewardBadge>("rewardBadges");
    public IMongoCollection<PremiumFeature> PremiumFeatures => _database.GetCollection<PremiumFeature>("premiumFeatures");
    public IMongoCollection<UserPremiumFeature> UserPremiumFeatures => _database.GetCollection<UserPremiumFeature>("userPremiumFeatures");

    // Settings Collections
    public IMongoCollection<SystemSetting> SystemSettings => _database.GetCollection<SystemSetting>("systemSettings");
    public IMongoCollection<UserSetting> UserSettings => _database.GetCollection<UserSetting>("userSettings");
    public IMongoCollection<StorageLocation> StorageLocations => _database.GetCollection<StorageLocation>("storageLocations");
    public IMongoCollection<FileStorageMapping> FileStorageMappings => _database.GetCollection<FileStorageMapping>("fileStorageMappings");

    // Audit & Logging Collections
    public IMongoCollection<AuditLog> AuditLogs => _database.GetCollection<AuditLog>("auditLogs");
    public IMongoCollection<ErrorLog> ErrorLogs => _database.GetCollection<ErrorLog>("errorLogs");
    public IMongoCollection<BackupHistory> BackupHistories => _database.GetCollection<BackupHistory>("backupHistories");
    public IMongoCollection<PerformanceMetric> PerformanceMetrics => _database.GetCollection<PerformanceMetric>("performanceMetrics");

    // Favorite Lists
    public IMongoCollection<FavoriteList> FavoriteLists => _database.GetCollection<FavoriteList>("favoriteLists");

    // Missing Features Collections
    public IMongoCollection<ContentModeration> ContentModeration => _database.GetCollection<ContentModeration>("contentModeration");
    public IMongoCollection<CopyrightManagement> CopyrightManagement => _database.GetCollection<CopyrightManagement>("copyrightManagement");
    public IMongoCollection<SearchHistory> SearchHistory => _database.GetCollection<SearchHistory>("searchHistory");
    public IMongoCollection<ContentSimilarity> ContentSimilarity => _database.GetCollection<ContentSimilarity>("contentSimilarity");
    public IMongoCollection<MediaProcessingJob> MediaProcessingJobs => _database.GetCollection<MediaProcessingJob>("mediaProcessingJobs");
    public IMongoCollection<CustomReport> CustomReports => _database.GetCollection<CustomReport>("customReports");
    public IMongoCollection<UserSecurity> UserSecurity => _database.GetCollection<UserSecurity>("userSecurity");
    public IMongoCollection<NotificationTemplate> NotificationTemplates => _database.GetCollection<NotificationTemplate>("notificationTemplates");
    public IMongoCollection<NotificationQueue> NotificationQueue => _database.GetCollection<NotificationQueue>("notificationQueue");
    public IMongoCollection<FileVersion> FileVersions => _database.GetCollection<FileVersion>("fileVersions");
    public IMongoCollection<FilePermission> FilePermissions => _database.GetCollection<FilePermission>("filePermissions");
    public IMongoCollection<UserGroup> UserGroups => _database.GetCollection<UserGroup>("userGroups");
    public IMongoCollection<UserActivityLog> UserActivityLogs => _database.GetCollection<UserActivityLog>("userActivityLogs");
    public IMongoCollection<SystemHealth> SystemHealth => _database.GetCollection<SystemHealth>("systemHealth");
    public IMongoCollection<SystemMaintenance> SystemMaintenance => _database.GetCollection<SystemMaintenance>("systemMaintenance");
}