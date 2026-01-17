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

        public async Task PublishDocumentUploadedAsync(DocumentUploadedMessage message, string exchange, string routingKey)
        {
            _logger.LogInformation("PublishDocumentUploadedAsync called for document {DocumentId}", message.DocumentId);
            try
            {
                var channel = await _rabbitMq.GetChannelAsync();

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
