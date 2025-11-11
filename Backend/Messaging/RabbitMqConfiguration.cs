namespace Swen3.API.Messaging
{
    public class RabbitMqConfiguration
    {
        public string HostName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }
        public int Port { get; set; }
        public string VirtualHost { get; set; }
    }
}
