using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Swen3.API.Messaging
{
    public class RabbitMqService : IRabbitMqService
    {
        private readonly RabbitMqConfiguration _config;
        private readonly IConnection _conn;
        private readonly IModel _channel;
        private bool _disposed;
        public RabbitMqService(IOptions<RabbitMqConfiguration> options)
        {
            _config = options.Value;

            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                Port = _config.Port,
                UserName = _config.Username,
                Password = _config.Password,
                VirtualHost = _config.VirtualHost
            };

            _conn = await factory.CreateConnectionAsync();
        }
    }
}
