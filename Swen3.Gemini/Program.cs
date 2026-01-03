using Swen3.Gemini.Services;

namespace Swen3.Gemini
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins("http://localhost:3030")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                    });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddScoped<GeminiService>();
            builder.Services.AddControllers();

            var app = builder.Build();
            app.UseRouting();
            app.UseCors(MyAllowSpecificOrigins);
            app.MapControllers();
            app.Run();
        }
    }
}

