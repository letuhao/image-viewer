using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageViewer.Domain.Events;
using ImageViewer.Infrastructure.Data;

namespace ImageViewer.Worker.Services;

/// <summary>
/// Base message consumer for RabbitMQ
/// </summary>
public abstract class BaseMessageConsumer : BackgroundService
{
    protected readonly IConnection _connection;
    protected readonly IModel _channel;
    protected readonly RabbitMQOptions _options;
    protected readonly ILogger _logger;
    protected readonly string _queueName;
    protected readonly string _consumerTag;

    protected BaseMessageConsumer(
        IConnection connection,
        IOptions<RabbitMQOptions> options,
        ILogger logger,
        string queueName,
        string consumerTag)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
        _queueName = queueName;
        _consumerTag = consumerTag;

        _channel = _connection.CreateModel();
        _channel.BasicQos(0, (ushort)_options.PrefetchCount, false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                
                _logger.LogInformation("Received message from queue {QueueName}: {Message}", _queueName, message);

                await ProcessMessageAsync(message, ea, stoppingToken);
                
                if (!_options.AutoAck)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", _queueName);
                
                if (!_options.AutoAck)
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue message
                }
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: _options.AutoAck,
            consumerTag: _consumerTag,
            consumer: consumer);

        _logger.LogInformation("Started consuming messages from queue {QueueName}", _queueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    protected abstract Task ProcessMessageAsync(string message, BasicDeliverEventArgs ea, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}
