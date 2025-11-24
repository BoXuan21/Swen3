using Microsoft.AspNetCore.Cors.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Swen3.Shared.Messaging;
using Swen3.Shared.OcrService;

namespace Swen3.API.Messaging
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IRabbitMqService _rabbitMq;

        public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger, IServiceScopeFactory scopeFactory, IRabbitMqService rabbitMqService)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _rabbitMq = rabbitMqService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await _rabbitMq.GetChannelAsync();
            await channel.QueueBindAsync(Topology.Queue, Topology.Exchange, Topology.RoutingKey);
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                using var scope = _scopeFactory.CreateScope();
                var ocrService = scope.ServiceProvider.GetRequiredService<IOcrService>();

                try
                {
                    var payload = JsonSerializer.Deserialize<DocumentUploadedMessage>(message);

                    await ocrService.ProcessDocumentForOcrAsync(payload, stoppingToken);

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message or run OCR. Re-queueing.");
                    // Reject and re-queue
                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
        }
    }
}
