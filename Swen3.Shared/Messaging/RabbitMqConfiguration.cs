namespace Swen3.Shared.Messaging
{
    public class RabbitMqConfiguration
    {
        public string HostName { get; set; } = "rabbitmq";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = Topology.Exchange;
        public string RoutingKey { get; set; } = Topology.RoutingKey;
        public string Queue { get; set; } = Topology.Queue;
        public string DeadLetterExchange { get; set; } = Topology.DeadLetterExchange;
        public string DeadLetterQueue { get; set; } = Topology.DeadLetterQueue;
    }
}
