using Swen3.Services.OcrService;
using Swen3.Shared.OcrService;
using Swen3.Services.Messaging;
using Swen3.Shared.Messaging;
using Swen3.Storage.MiniIo;

namespace Swen3.Services
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
            builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            builder.Services.AddHostedService<OcrConsumer>();

            builder.Services.AddScoped<IOcrService, TesseractOcrService>();

            builder.Services
                .AddOptions<MinioOptions>()
                .Bind(builder.Configuration.GetSection("Minio"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "MinIO endpoint must be configured")
                .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "MinIO bucket name must be configured")
                .ValidateOnStart();

            builder.Services.AddSingleton<IDocumentStorageService, MinioDocumentStorageService>();

            var host = builder.Build();
            host.Run();
        }
    }
}
