namespace Swen3.Shared.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishDocumentUploadedAsync(DocumentUploadedMessage message, string exchange, string routingKey);
    }
}
