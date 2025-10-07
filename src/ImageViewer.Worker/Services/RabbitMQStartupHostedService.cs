using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ImageViewer.Infrastructure.Services;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Hosted service that sets up RabbitMQ queues and exchanges on startup
/// </summary>
public class RabbitMQStartupHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQStartupHostedService> _logger;

    public RabbitMQStartupHostedService(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQStartupHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting RabbitMQ setup...");

            using var scope = _serviceProvider.CreateScope();
            var setupService = scope.ServiceProvider.GetRequiredService<RabbitMQSetupService>();
            
            await setupService.SetupQueuesAndExchangesAsync();
            
            _logger.LogInformation("RabbitMQ setup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup RabbitMQ queues and exchanges");
            throw;
        }
    }
}
