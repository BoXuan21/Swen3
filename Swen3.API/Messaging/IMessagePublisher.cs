using Swen3.Shared.Messaging;

namespace Swen3.API.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishDocumentUploadedAsync(DocumentUploadedMessage message);
    }
}
