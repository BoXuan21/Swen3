using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Swen3.Shared.Messaging
{
    public class RabbitMqService : IRabbitMqService, IDisposable
    {
        private readonly RabbitMqConfiguration _config;
        private IConnection _conn;
        private IChannel _channel;
        private ILogger<RabbitMqService> _logger;
        private bool _disposed;
        public RabbitMqService(IOptions<RabbitMqConfiguration> options, ILogger<RabbitMqService> logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        public async Task<IChannel> GetChannelAsync()
        {
            try
            {
                // Initialize connection if needed
                if (_conn == null || !_conn.IsOpen)
                {
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

                // Initialize channel if needed
                if (_channel == null || _channel.IsClosed)
                {
                    _channel = await _conn.CreateChannelAsync();
                }

                return _channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting channel");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _channel?.CloseAsync();
                _channel?.Dispose();
            }
            catch { }

            try
            {
                _conn?.CloseAsync();
                _conn?.Dispose();
            }
            catch { }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
