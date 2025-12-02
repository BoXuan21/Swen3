using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Swen3.Shared.Messaging;
using Swen3.Shared.OcrService;

namespace Swen3.Services.Messaging
{
    public class OcrConsumer : BackgroundService
    {
        private readonly ILogger<OcrConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqService _rabbitMq;
        private bool _topologyInitialized;

        public OcrConsumer(ILogger<OcrConsumer> logger, IServiceScopeFactory scopeFactory, IRabbitMqService rabbitMqService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _rabbitMq = rabbitMqService;
            _logger.LogInformation("RabbitMqConsumer initialized!");
        }

        private async Task EnsureTopologyAsync(IChannel channel)
        {
            if (_topologyInitialized)
            {
                _logger.LogDebug("Topology already initialized, skipping");
                return;
            }
            try
            {
                if (_topologyInitialized) return;

                _logger.LogInformation("Starting topology initialization...");

                // Declare dead letter exchange
                await channel.ExchangeDeclareAsync(
                    exchange: Topology.DeadLetterExchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false
                );

                // Declare dead letter queue
                await channel.QueueDeclareAsync(
                    queue: Topology.DeadLetterQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // Bind DLQ to DLX
                await channel.QueueBindAsync(
                    queue: Topology.DeadLetterQueue,
                    exchange: Topology.DeadLetterExchange,
                    routingKey: Topology.DeadLetterQueue
                );

                // Declare main exchange
                await channel.ExchangeDeclareAsync(
                    exchange: Topology.Exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                // Declare main queue with DLX configuration
                var queueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", Topology.DeadLetterExchange },
                    { "x-dead-letter-routing-key", Topology.DeadLetterQueue }
                };

                await channel.QueueDeclareAsync(
                    queue: Topology.Queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs
                );

                // Bind main queue to exchange
                await channel.QueueBindAsync(
                    queue: Topology.Queue,
                    exchange: Topology.Exchange,
                    routingKey: Topology.RoutingKey
                );

                _topologyInitialized = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ensure topology");
                throw;
            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Received call!");
            var channel = await _rabbitMq.GetChannelAsync();

            await EnsureTopologyAsync(channel);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                _logger.LogInformation("Received message!");
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                _logger.LogInformation("Proceeding to process document");
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };

                    var payload = JsonSerializer.Deserialize<DocumentUploadedMessage>(message, options);

                    _logger.LogInformation("Calling process document");
                    _logger.LogInformation("Message received in consumer: {Message}", payload);
                    using var scope = _scopeFactory.CreateScope();
                    var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();
                    await ocrService.ProcessDocumentForOcrAsync(payload, stoppingToken);

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message or run OCR. Re-queueing.");
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(queue: Topology.Queue, autoAck: false, consumer: consumer);
            _logger.LogInformation("Consumer is now listening on queue: {Queue}", Topology.Queue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
