namespace Swen3.API.Common.Exceptions
{
    /// <summary>
    /// Exception for messaging service errors (RabbitMQ, etc.)
    /// Maps to HTTP 503 Service Unavailable
    /// </summary>
    public class MessagingException : Exception
    {
        public MessagingException(string message) : base(message) { }
        
        public MessagingException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}

