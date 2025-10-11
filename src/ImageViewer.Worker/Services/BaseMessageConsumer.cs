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
    protected readonly IChannel _channel;
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

        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
        // Set prefetch count to limit concurrent message processing
        // This prevents overwhelming system resources (CPU, disk I/O, memory)
        // PrefetchCount=10 means: Process max 10 messages concurrently per consumer
        // Lower = safer (less resource usage), Higher = faster (more throughput)
        _channel.BasicQosAsync(0, (ushort)_options.PrefetchCount, false).GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncChannelConsumer(_channel, _options, _logger, async (msg, ct) =>
        {
            await ProcessMessageAsync(msg, ct);
        });

        await _channel.BasicConsumeAsync(
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

    protected abstract Task ProcessMessageAsync(string message, CancellationToken cancellationToken);

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        base.Dispose();
    }

    private sealed class AsyncChannelConsumer : IAsyncBasicConsumer
    {
        private readonly IChannel _channel;
        private readonly RabbitMQOptions _options;
        private readonly ILogger _logger;
        private readonly Func<string, CancellationToken, Task> _handler;

        public AsyncChannelConsumer(
            IChannel channel,
            RabbitMQOptions options,
            ILogger logger,
            Func<string, CancellationToken, Task> handler)
        {
            _channel = channel;
            _options = options;
            _logger = logger;
            _handler = handler;
        }

        public IChannel Channel => _channel;

        public Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task HandleChannelShutdownAsync(object model, ShutdownEventArgs reason) => Task.CompletedTask;

        public async Task HandleBasicDeliverAsync(
            string consumerTag,
            ulong deliveryTag,
            bool redelivered,
            string exchange,
            string routingKey,
            IReadOnlyBasicProperties properties,
            ReadOnlyMemory<byte> body,
            CancellationToken cancellationToken = default)
        {
            var message = Encoding.UTF8.GetString(body.ToArray());
            _logger.LogInformation("Received message from queue {ConsumerTag}: {Message}", consumerTag, message);

            try
            {
                await _handler(message, cancellationToken);

                if (!_options.AutoAck)
                {
                    await _channel.BasicAckAsync(deliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message (deliveryTag: {DeliveryTag})", deliveryTag);
                if (!_options.AutoAck)
                {
                    await _channel.BasicNackAsync(deliveryTag, false, true);
                }
            }
        }
    }
}
