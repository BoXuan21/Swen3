using Swen3.Shared.Messaging;

namespace Swen3.Shared.OcrService
{
    public interface IOcrService
    {
        Task ProcessDocumentForOcrAsync(DocumentUploadedMessage message, CancellationToken cancellationToken);
    }
}
