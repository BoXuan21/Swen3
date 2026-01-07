using Moq;
using Swen3.Services.OcrService;
using Swen3.Shared.Messaging;
using Swen3.Storage.MiniIo;
using Microsoft.Extensions.Logging;
using Swen3.Shared.Elasticsearch;

namespace Swen3.Test;

/// <summary>
/// Mock setup for OCR Worker Service tests
/// </summary>
public class OcrServiceMocks
{
    public Mock<ILogger<TesseractOcrService>> MockLogger { get; }
    public Mock<IDocumentStorageService> MockStorage { get; }
    public Mock<IMessagePublisher> MockPublisher { get; }
    public Mock<IElasticsearchService> MockElasticsearch { get; }

    public OcrServiceMocks()
    {
        MockLogger = new Mock<ILogger<TesseractOcrService>>();
        MockStorage = new Mock<IDocumentStorageService>();
        MockPublisher = new Mock<IMessagePublisher>();
        MockElasticsearch = new Mock<IElasticsearchService>();

        // Default: Elasticsearch succeeds
        MockElasticsearch.Setup(es => es.IndexDocumentAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    public TesseractOcrService CreateService()
    {
        return new TesseractOcrService(
            MockLogger.Object,
            MockStorage.Object,
            MockPublisher.Object,
            MockElasticsearch.Object
        );
    }

    public void SetupSuccessfulDownload()
    {
        MockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 })); // PDF magic bytes
    }
}
