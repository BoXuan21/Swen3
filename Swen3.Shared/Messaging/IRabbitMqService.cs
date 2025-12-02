using RabbitMQ.Client;

namespace Swen3.Shared.Messaging
{
    public interface IRabbitMqService
    {
        Task<IChannel> GetChannelAsync();
    }
}
