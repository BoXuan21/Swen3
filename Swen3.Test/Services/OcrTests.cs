using Moq;
using Swen3.Services.OcrService;
using Swen3.Shared.Messaging;
using Swen3.Storage.MiniIo;
using Swen3.Test;
using Microsoft.Extensions.Logging;
using Assert = NUnit.Framework.Assert; // Use NUnit Assert

namespace Swen3.Services.Tests
{
    public class TestableTesseractOcrService : TesseractOcrService
    {
        public Action ConvertAction { get; set; }
        public Func<string> OcrFunc { get; set; } = () => "Mock OCR Result";

        public bool IsCleanupCalled { get; private set; }
        public bool IsFileSaved { get; private set; }

        public TestableTesseractOcrService(ILogger<TesseractOcrService> logger, IDocumentStorageService storage, IMessagePublisher publisher)
            : base(logger, storage, publisher) { }

        protected override void ConvertPdfToTiffWithImageMagick(string inputPath, string outputPath)
        {
            ConvertAction?.Invoke();

            if (!IsCleanupCalled)
            {
                File.WriteAllText(outputPath, "Mock TIFF Content");
            }
        }

        protected override string RunOcrWithTesseract(string imagePath)
        {
            return OcrFunc();
        }

        protected override void DeleteFileIfExists(string path)
        {
            IsCleanupCalled = true;
            return;
        }

        protected override async Task DownloadAndSavePdf(string objectKey, string localPath, CancellationToken cancellationToken)
        {
            await base.DownloadAndSavePdf(objectKey, localPath, cancellationToken);
            IsFileSaved = true;
        }
    }

    [TestFixture]
    public class TesseractOcrServiceTests
    {
        private OcrServiceMocks _mocks = null!;
        private TestableTesseractOcrService _service = null!;
        private DocumentUploadedMessage _testMessage = null!;

        private const string ExpectedOcrResult = "Unit Test OCR Text";

        private void VerifyLog(LogLevel level, string messageContains, Times times, Type? exceptionType = null)
        {
            _mocks.MockLogger.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString().Contains(messageContains)), // Checks the message content
                    It.Is<Exception>(ex => exceptionType == null || ex.GetType() == exceptionType), // Checks exception type
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                times,
                $"Expected log at {level} containing '{messageContains}' to be called {times}.");
        }

        [SetUp]
        public void Setup()
        {
            _mocks = new OcrServiceMocks();
            var logger = new Mock<ILogger<TesseractOcrService>>();

            _service = new TestableTesseractOcrService(
                logger.Object,
                _mocks.MockStorage.Object,
                _mocks.MockPublisher.Object
            );

            _testMessage = new DocumentUploadedMessage(
                DocumentId: Guid.NewGuid(),
                FileName: "test-document.pdf",
                ContentType: "application/pdf",
                UploadedAtUtc: DateTime.UtcNow,
                StoragePath: "2025/12/05/key.pdf",
                Metadata: "",
                CorrelationId: Guid.NewGuid().ToString(),
                TenantId: "T1",
                Version: 1
            );

            _mocks.SetupSuccessfulDownload();
            _service.OcrFunc = () => ExpectedOcrResult;
            _service.ConvertAction = null;
        }

        [Test]
        public async Task ProcessDocumentForOcrAsync_Success_CallsAllStepsAndPublishesUpdatedMessage()
        {
            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert

            // 1. Verify MinIO Download was called
            _mocks.MockStorage.Verify(s =>
                s.DownloadAsync(_testMessage.StoragePath, It.IsAny<CancellationToken>()),
                Times.Once, "DownloadAsync must be called to start the process.");

            // 2. Verify Publisher was called with the CORRECT result (Metadata update)
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                // Assert that the message sent has the expected OCR result injected into the Metadata field
                It.Is<DocumentUploadedMessage>(msg =>
                    msg.DocumentId == _testMessage.DocumentId &&
                    msg.Metadata == ExpectedOcrResult),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Once, "The updated message must be published.");

            // 3. Verify Cleanup was called
            Assert.IsTrue(_service.IsCleanupCalled, "Cleanup (DeleteFileIfExists) must be called in the finally block.");
        }
    }
}
