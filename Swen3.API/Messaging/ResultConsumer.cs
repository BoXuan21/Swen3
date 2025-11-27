using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Swen3.Shared.Messaging;
using Swen3.API.DAL.Interfaces;

namespace Swen3.API.Messaging
{
    public class ResultConsumer : BackgroundService
    {
        private readonly ILogger<ResultConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqService _rabbitMq;
        private bool _topologyInitialized;

        public ResultConsumer(ILogger<ResultConsumer> logger, IServiceScopeFactory scopeFactory, IRabbitMqService rabbitMqService)
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
                    exchange: Topology.ResultDLX,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false
                );

                // Declare dead letter queue
                await channel.QueueDeclareAsync(
                    queue: Topology.ResultDLQ,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                // Bind DLQ to DLX
                await channel.QueueBindAsync(
                    queue: Topology.ResultDLQ,
                    exchange: Topology.ResultDLX,
                    routingKey: Topology.ResultDLQ
                );

                // Declare main exchange
                await channel.ExchangeDeclareAsync(
                    exchange: Topology.ResultExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                // Declare main queue with DLX configuration
                var queueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", Topology.ResultDLX },
                    { "x-dead-letter-routing-key", Topology.ResultDLQ }
                };

                await channel.QueueDeclareAsync(
                    queue: Topology.ResultQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: queueArgs
                );

                // Bind main queue to exchange
                await channel.QueueBindAsync(
                    queue: Topology.ResultQueue,
                    exchange: Topology.ResultExchange,
                    routingKey: Topology.ResultRoutingKey
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

                using var scope = _scopeFactory.CreateScope();
                var resultHandler = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };

                    var payload = JsonSerializer.Deserialize<DocumentUploadedMessage>(message, options);

                    if (payload == null)
                    {
                        _logger.LogError("Result message is empty!");
                        throw new NullReferenceException("Empty result message");
                    }

                    var document = await resultHandler.GetByIdAsync(payload.DocumentId);
                    _logger.LogInformation("Document from repo: {Document}", document);
                    document.Metadata = payload.Metadata;

                    await resultHandler.UpdateAsync(document);

                    _logger.LogInformation("Updated document's metadata!");

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message or run OCR. Re-queueing.");
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            };

            await channel.BasicConsumeAsync(queue: Topology.ResultQueue, autoAck: false, consumer: consumer);
            _logger.LogInformation("Consumer is now listening on queue: {Queue}", Topology.ResultQueue);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
