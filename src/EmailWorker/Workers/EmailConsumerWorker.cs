using System.Text;
using System.Text.Json;
using EmailWorker.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configuration;
using Shared.Events;

namespace EmailWorker.Workers;

public sealed class EmailConsumerWorker : BackgroundService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly RabbitMqSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailConsumerWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public EmailConsumerWorker(
        IOptions<RabbitMqSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailConsumerWorker> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);

        await _channel!.QueueDeclareAsync(
            queue: _settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Process one message at a time
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: _settings.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("EmailConsumerWorker started. Listening on queue '{Queue}'.", _settings.QueueName);

        // Keep the worker alive until cancellation is requested
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("EmailConsumerWorker is stopping.");
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogInformation(
                "Message received from queue '{Queue}'. DeliveryTag: {DeliveryTag}",
                _settings.QueueName, deliveryTag);

            _logger.LogDebug("Raw message payload: {RawMessage}", json);

            var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(json, _jsonOptions);

            if (userCreatedEvent is null)
            {
                _logger.LogError(
                    "Deserialization returned null for DeliveryTag: {DeliveryTag}. Raw payload: {RawMessage}",
                    deliveryTag, json);
                await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
                return;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            await emailService.SendWelcomeEmailAsync(userCreatedEvent);

            await _channel!.BasicAckAsync(deliveryTag, multiple: false);

            _logger.LogInformation(
                "Message acknowledged. DeliveryTag: {DeliveryTag}", deliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message. DeliveryTag: {DeliveryTag}. Message will be requeued.",
                deliveryTag);

            await _channel!.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 5;
        const int delaySeconds = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Connected to RabbitMQ at {Host}:{Port}", _settings.Host, _settings.Port);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ connection attempt {Attempt}/{MaxRetries} failed. Retrying in {Delay}s...",
                    attempt, maxRetries, delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        throw new InvalidOperationException(
            $"Could not connect to RabbitMQ after {maxRetries} attempts.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel is not null)
            await _channel.DisposeAsync();

        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
