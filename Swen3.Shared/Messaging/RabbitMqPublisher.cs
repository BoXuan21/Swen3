using RabbitMQ.Client;
using System.Text;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Swen3.Shared.Messaging
{
    public class RabbitMqPublisher : IMessagePublisher
    {
        private readonly IRabbitMqService _rabbitMq;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private bool _topologyInitialized;

        public RabbitMqPublisher(IRabbitMqService rabbitMq, ILogger<RabbitMqPublisher> logger)
        {
            _rabbitMq = rabbitMq;
            _logger = logger;
            _logger.LogInformation("RabbitMqPublisher initialized");
        }

        /*private async Task EnsureTopologyAsync(IChannel channel)
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
        }*/

        public async Task PublishDocumentUploadedAsync(DocumentUploadedMessage message, string exchange, string routingKey)
        {
            _logger.LogInformation("PublishDocumentUploadedAsync called for document {DocumentId}", message.DocumentId);
            try
            {
                var channel = await _rabbitMq.GetChannelAsync();

                //await EnsureTopologyAsync(channel);

                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json",
                    MessageId = Guid.NewGuid().ToString(),
                    CorrelationId = message.CorrelationId,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                    Headers = new Dictionary<string, object?>
                    {
                        { "message-type", nameof(DocumentUploadedMessage) },
                        { "version", message.Version }
                    }
                };

                if (!string.IsNullOrEmpty(message.TenantId))
                {
                    properties.Headers["tenant-id"] = message.TenantId;
                }

                await channel.BasicPublishAsync(
                    exchange: exchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing document message");
                throw;
            }
        }
    }
}
