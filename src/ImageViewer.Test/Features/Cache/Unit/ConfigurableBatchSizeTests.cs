using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Test.Features.Cache.Unit;

/// <summary>
/// Unit tests for configurable batch size and queue limits
/// 可配置批处理大小单元测试 - Kiểm thử đơn vị kích thước batch cấu hình
/// </summary>
public class ConfigurableBatchSizeTests
{
    [Fact]
    public void RabbitMQOptions_HasCorrectDefaults()
    {
        // Arrange & Act
        var options = new RabbitMQOptions();

        // Assert
        Assert.Equal(100, options.MessageBatchSize);
        Assert.Equal(100000, options.MaxQueueLength);
        Assert.Equal(500 * 1024 * 1024, options.MaxImageSizeBytes); // 500MB
        Assert.Equal(20L * 1024 * 1024 * 1024, options.MaxZipEntrySizeBytes); // 20GB
        Assert.Equal(10, options.PrefetchCount);
        Assert.False(options.AutoAck);
    }

    [Fact]
    public void RabbitMQOptions_CanBeConfigured()
    {
        // Arrange
        var options = new RabbitMQOptions
        {
            MessageBatchSize = 200,
            MaxQueueLength = 50000,
            MaxImageSizeBytes = 1024 * 1024 * 1024, // 1GB
            MaxZipEntrySizeBytes = 10L * 1024 * 1024 * 1024, // 10GB
            PrefetchCount = 20
        };

        // Assert
        Assert.Equal(200, options.MessageBatchSize);
        Assert.Equal(50000, options.MaxQueueLength);
        Assert.Equal(1024 * 1024 * 1024, options.MaxImageSizeBytes);
        Assert.Equal(10L * 1024 * 1024 * 1024, options.MaxZipEntrySizeBytes);
        Assert.Equal(20, options.PrefetchCount);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(500, 500)]
    [InlineData(1000, 1000)]
    public void MessageBatchSize_AcceptsValidValues(int batchSize, int expected)
    {
        // Arrange & Act
        var options = new RabbitMQOptions { MessageBatchSize = batchSize };

        // Assert
        Assert.Equal(expected, options.MessageBatchSize);
    }

    [Theory]
    [InlineData(10000, 10000)]
    [InlineData(100000, 100000)]
    [InlineData(500000, 500000)]
    public void MaxQueueLength_AcceptsValidValues(int maxLength, int expected)
    {
        // Arrange & Act
        var options = new RabbitMQOptions { MaxQueueLength = maxLength };

        // Assert
        Assert.Equal(expected, options.MaxQueueLength);
    }

    [Theory]
    [InlineData(100 * 1024 * 1024, 100 * 1024 * 1024)] // 100MB
    [InlineData(500 * 1024 * 1024, 500 * 1024 * 1024)] // 500MB
    [InlineData(1024 * 1024 * 1024, 1024 * 1024 * 1024)] // 1GB
    public void MaxImageSizeBytes_AcceptsValidValues(long size, long expected)
    {
        // Arrange & Act
        var options = new RabbitMQOptions { MaxImageSizeBytes = size };

        // Assert
        Assert.Equal(expected, options.MaxImageSizeBytes);
    }

    [Theory]
    [InlineData(5L * 1024 * 1024 * 1024, 5L * 1024 * 1024 * 1024)]   // 5GB
    [InlineData(10L * 1024 * 1024 * 1024, 10L * 1024 * 1024 * 1024)] // 10GB
    [InlineData(20L * 1024 * 1024 * 1024, 20L * 1024 * 1024 * 1024)] // 20GB
    [InlineData(50L * 1024 * 1024 * 1024, 50L * 1024 * 1024 * 1024)] // 50GB
    public void MaxZipEntrySizeBytes_AcceptsValidValues(long size, long expected)
    {
        // Arrange & Act
        var options = new RabbitMQOptions { MaxZipEntrySizeBytes = size };

        // Assert
        Assert.Equal(expected, options.MaxZipEntrySizeBytes);
    }

    [Fact]
    public void MaxZipEntrySizeBytes_IsLargerThanMaxImageSize()
    {
        // Arrange
        var options = new RabbitMQOptions();

        // Assert: ZIP entries should have higher limit than regular files
        Assert.True(options.MaxZipEntrySizeBytes > options.MaxImageSizeBytes,
            "ZIP entry limit should be higher than regular file limit");
        
        // Verify specific ratio
        var ratio = (double)options.MaxZipEntrySizeBytes / options.MaxImageSizeBytes;
        Assert.True(ratio >= 40, $"ZIP limit should be at least 40x regular limit, actual: {ratio}x");
    }
}

