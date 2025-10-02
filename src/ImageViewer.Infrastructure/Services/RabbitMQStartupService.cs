using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Infrastructure.Services;

/// <summary>
/// Hosted service that sets up RabbitMQ queues and exchanges on startup
/// </summary>
public class RabbitMQStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQStartupService> _logger;
    private readonly RabbitMQOptions _options;

    public RabbitMQStartupService(
        IServiceProvider serviceProvider,
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting RabbitMQ setup...");

            using var scope = _serviceProvider.CreateScope();
            var connection = scope.ServiceProvider.GetRequiredService<IConnection>();
            var setupService = new RabbitMQSetupService(connection, Options.Create(_options), 
                scope.ServiceProvider.GetRequiredService<ILogger<RabbitMQSetupService>>());

            // Check if queues already exist
            var queuesExist = await setupService.CheckQueuesExistAsync();
            
            if (!queuesExist)
            {
                _logger.LogInformation("Queues do not exist, creating them...");
                await setupService.SetupQueuesAndExchangesAsync();
            }
            else
            {
                _logger.LogInformation("All required queues already exist");
            }

            _logger.LogInformation("RabbitMQ setup completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set up RabbitMQ queues and exchanges");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ startup service stopped");
        return Task.CompletedTask;
    }
}
