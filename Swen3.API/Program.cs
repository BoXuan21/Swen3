using Swen3.Shared.Messaging;
using Microsoft.Extensions.Options;
using Swen3.API.DAL;
using Swen3.API.Messaging;
using Swen3.API.DAL.Mapping;
using Swen3.API.Middleware;
using Swen3.Storage.MiniIo;

namespace Swen3.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSwenDal(builder.Configuration);
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            builder.Services.AddAutoMapper(typeof(DocumentProfile).Assembly);

            builder.Services
                .AddOptions<MinioOptions>()
                .Bind(builder.Configuration.GetSection("Minio"))
                .ValidateDataAnnotations()
                .Validate(o => !string.IsNullOrWhiteSpace(o.Endpoint), "MinIO endpoint must be configured")
                .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "MinIO bucket name must be configured")
                .ValidateOnStart();

            builder.Services.AddSingleton<IDocumentStorageService, MinioDocumentStorageService>();

            // Add RabbitMq
            builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("Messaging"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqConfiguration>>().Value);
            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
            builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Register exception handling middleware (should be early in pipeline)
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
