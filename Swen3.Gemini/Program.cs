using Swen3.Gemini.Services;

namespace Swen3.Gemini
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<IGeminiService, GeminiService>();

            var app = builder.Build();
            app.Run();
        }
    }
}

