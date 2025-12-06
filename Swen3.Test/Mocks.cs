using Moq;
using Swen3.Shared.Messaging;
using Swen3.Storage.MiniIo;
using Swen3.Services.OcrService;
using Microsoft.Extensions.Logging;

namespace Swen3.Test;
// Utility class to easily set up standard mock dependencies
public class OcrServiceMocks
{
    public Mock<ILogger<TesseractOcrService>> MockLogger { get; }
    public Mock<IDocumentStorageService> MockStorage { get; }
    public Mock<IMessagePublisher> MockPublisher { get; }

    public OcrServiceMocks()
    {
        MockLogger = new Mock<ILogger<TesseractOcrService>>();
        MockStorage = new Mock<IDocumentStorageService>();
        MockPublisher = new Mock<IMessagePublisher>();
    }

    public TesseractOcrService CreateService()
    {
        return new TesseractOcrService(
            MockLogger.Object,
            MockStorage.Object,
            MockPublisher.Object
        );
    }

    /// <summary>
    /// Sets up the storage mock to simulate a successful PDF download.
    /// </summary>
    public void SetupSuccessfulDownload()
    {
        // Create a non-empty, disposable MemoryStream to simulate the PDF file content.
        var mockStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        MockStorage.Setup(s =>
            s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockStream);
    }
}
