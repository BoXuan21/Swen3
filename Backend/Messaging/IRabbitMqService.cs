using RabbitMQ.Client;

namespace Swen3.API.Messaging
{
    public interface IRabbitMqService
    {
        Task<IChannel> GetChannelAsync();
    }
}
