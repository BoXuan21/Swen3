using Swen3.Services.OcrService;
using Swen3.Shared.OcrService;
using Swen3.Services.Messaging;
using Swen3.Shared.Messaging;
using Swen3.Shared.Elasticsearch;
using Swen3.Storage.MiniIo;

namespace Swen3.Services
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // RabbitMQ configuration
            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
            builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OcrConsumer>();

            // OCR service configuration
            builder.Services.AddScoped<IOcrService, TesseractOcrService>();

            // Minio configuration
            builder.Services
                .AddOptions<MinioOptions>()
                .Bind(builder.Configuration.GetSection("Minio"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "MinIO endpoint must be configured")
                .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "MinIO bucket name must be configured")
                .ValidateOnStart();

            builder.Services.AddSingleton<IDocumentStorageService, MinioDocumentStorageService>();

            // Elasticsearch configuration
            builder.Services
                .AddOptions<ElasticsearchOptions>()
                .Bind(builder.Configuration.GetSection("Elasticsearch"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

            var host = builder.Build();
            host.Run();
        }
    }
}
