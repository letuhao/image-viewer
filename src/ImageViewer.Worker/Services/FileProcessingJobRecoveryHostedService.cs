using ImageViewer.Application.Services;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Hosted service that automatically recovers incomplete file processing jobs on worker startup
/// 文件处理任务恢复托管服务 - Dịch vụ khôi phục công việc xử lý file tự động
/// </summary>
public class FileProcessingJobRecoveryHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileProcessingJobRecoveryHostedService> _logger;

    public FileProcessingJobRecoveryHostedService(
        IServiceProvider serviceProvider,
        ILogger<FileProcessingJobRecoveryHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 FileProcessingJobRecoveryHostedService starting...");
        
        // Wait 3 seconds for other services to initialize (reduced from 5 seconds)
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Startup cancelled during initialization delay, skipping job recovery");
            return;
        }
        
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("⚠️ Cancellation requested, skipping job recovery");
            return;
        }
        
        try
        {
            _logger.LogInformation("🔄 Starting automatic file processing job recovery...");
            
            using var scope = _serviceProvider.CreateScope();
            var recoveryService = scope.ServiceProvider.GetRequiredService<IFileProcessingJobRecoveryService>();
            
            // Recover all incomplete jobs (cache, thumbnail, etc.)
            await recoveryService.RecoverIncompleteJobsAsync();
            
            _logger.LogInformation("✅ Automatic job recovery completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("⚠️ Job recovery cancelled during execution");
            // Don't throw - worker should continue even if recovery is cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to recover incomplete jobs on startup");
            // Don't throw - worker should continue even if recovery fails
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️ FileProcessingJobRecoveryHostedService stopping...");
        return Task.CompletedTask;
    }
}

