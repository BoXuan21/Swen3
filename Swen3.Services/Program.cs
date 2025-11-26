using Swen3.Services.OcrService;
using Swen3.Shared.OcrService;
using Swen3.Services.Messaging;
using Swen3.Shared.Messaging;

namespace Swen3.Services
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
            builder.Services.AddHostedService<RabbitMqConsumer>();

            builder.Services.AddScoped<IOcrService, TesseractOcrService>();

            var host = builder.Build();
            host.Run();
        }
    }
}
