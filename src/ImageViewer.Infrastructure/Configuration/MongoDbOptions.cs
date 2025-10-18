namespace ImageViewer.Infrastructure.Configuration;

/// <summary>
/// MongoDB configuration options
/// </summary>
public class MongoDbOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int ServerSelectionTimeoutSeconds { get; set; } = 30;
    public int SocketTimeoutSeconds { get; set; } = 30;
    public int MaxPoolSize { get; set; } = 100;
    public int MinPoolSize { get; set; } = 5;
    public int MaxIdleTimeSeconds { get; set; } = 300;
    public bool RetryWrites { get; set; } = true;
    public bool RetryReads { get; set; } = true;
}
