
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Swen3.API.DAL;
using Swen3.API.DAL.Mapping;
using Swen3.API.Messaging;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSwenDal(builder.Configuration);

            // Added AutoMapper
            // Replace this line:
            // builder.Services.AddSingleton(sp => sp.GetRequiredKeyedService<IOptions<RabbitMqConfiguration>>().Value);

            // With this line:
            builder.Services.AddAutoMapper(typeof(Program));
            
            // Add RabbitMq
            builder.Services.Configure<RabbitMqConfiguration>(builder.Configuration.GetSection("RabbitMq"));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMqConfiguration>>().Value);

            builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

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

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
