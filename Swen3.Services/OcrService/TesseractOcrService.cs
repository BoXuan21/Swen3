using Swen3.Shared.Messaging;
using Swen3.Shared.OcrService;

namespace Swen3.Services.OcrService
{
    public class TesseractOcrService : IOcrService
    {
        private readonly ILogger<TesseractOcrService> _logger;
        public TesseractOcrService(ILogger<TesseractOcrService> logger)
        {
            _logger = logger;
        }
        public Task ProcessDocumentForOcrAsync(DocumentUploadedMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing document {DocumentId} for OCR", message.DocumentId);

            throw new NotImplementedException();
        }
    }
}
