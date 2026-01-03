using Swen3.Shared.Messaging;
using Swen3.Shared.Elasticsearch;
using Microsoft.Extensions.Options;
using Swen3.API.DAL;
using Swen3.API.DAL.Mapping;
using Swen3.API.Middleware;
using Swen3.Storage.MiniIo;
using Swen3.API.Messaging;

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

            // Add Elasticsearch
            builder.Services
                .AddOptions<ElasticsearchOptions>()
                .Bind(builder.Configuration.GetSection("Elasticsearch"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            builder.Services.AddSingleton<IElasticsearchService, ElasticsearchService>();

            // Add RabbitMq
            builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("Messaging"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqConfiguration>>().Value);
            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();
            builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

            builder.Services.AddHostedService<ResultConsumer>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
