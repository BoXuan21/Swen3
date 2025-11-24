using Swen3.Services.OcrService;
using Swen3.Shared.OcrService;

namespace Swen3.Services
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddScoped<IOcrService, TesseractOcrService>();

            var host = builder.Build();
            host.Run();
        }
    }
}