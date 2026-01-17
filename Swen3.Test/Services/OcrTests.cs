using Moq;
using Swen3.Services.OcrService;
using Swen3.Shared.Messaging;
using Swen3.Shared.Elasticsearch;
using Swen3.Storage.MiniIo;
using Swen3.Test;
using Microsoft.Extensions.Logging;
using Assert = NUnit.Framework.Assert;

namespace Swen3.Services.Tests
{
    /// <summary>
    /// Testable version of TesseractOcrService that mocks external dependencies
    /// </summary>
    public class TestableTesseractOcrService : TesseractOcrService
    {
        public Func<string> OcrFunc { get; set; } = () => "Mock OCR Result";
        public bool IsCleanupCalled { get; private set; }

        public TestableTesseractOcrService(
            ILogger<TesseractOcrService> logger,
            IDocumentStorageService storage,
            IMessagePublisher publisher,
            IElasticsearchService elasticsearchService)
            : base(logger, storage, publisher, elasticsearchService) { }

        protected override void ConvertPdfToTiffWithImageMagick(string inputPath, string outputPath)
        {
            var directory = Path.GetDirectoryName(outputPath);
            var fileNameBase = Path.GetFileNameWithoutExtension(outputPath);
            var dummyPagePath = Path.Combine(directory!, $"{fileNameBase}-0.tiff");
            File.WriteAllText(dummyPagePath, "Mock TIFF Content");
        }

        protected override string RunOcrWithTesseract(string imagePath) => OcrFunc();

        protected override void DeleteFileIfExists(string path)
        {
            IsCleanupCalled = true;
        }

        protected override async Task DownloadAndSavePdf(string objectKey, string localPath, CancellationToken cancellationToken)
        {
            await base.DownloadAndSavePdf(objectKey, localPath, cancellationToken);
        }
    }

    /// <summary>
    /// Unit tests for OCR Worker Service with Elasticsearch integration
    /// </summary>
    [TestFixture]
    public class TesseractOcrServiceTests
    {
        private OcrServiceMocks _mocks = null!;
        private TestableTesseractOcrService _service = null!;
        private DocumentUploadedMessage _testMessage = null!;
        private const string ExpectedOcrResult = "Invoice number 12345 for consulting services";

        [SetUp]
        public void Setup()
        {
            _mocks = new OcrServiceMocks();
            var logger = new Mock<ILogger<TesseractOcrService>>();

            _service = new TestableTesseractOcrService(
                logger.Object,
                _mocks.MockStorage.Object,
                _mocks.MockPublisher.Object,
                _mocks.MockElasticsearch.Object
            );

            _testMessage = new DocumentUploadedMessage(
                DocumentId: Guid.NewGuid(),
                FileName: "invoice-2024.pdf",
                ContentType: "application/pdf",
                UploadedAtUtc: DateTime.UtcNow,
                StoragePath: "2025/12/05/key.pdf",
                Metadata: "",
                Summary: "",
                CorrelationId: Guid.NewGuid().ToString(),
                TenantId: "T1",
                Version: 1
            );

            _mocks.SetupSuccessfulDownload();
            _service.OcrFunc = () => ExpectedOcrResult;
        }

        /// <summary>
        /// TEST 1: Verify the complete OCR pipeline executes successfully
        /// </summary>
        [Test]
        public async Task ProcessDocumentForOcrAsync_Success_CompletesAllSteps()
        {
            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert - Verify all steps were executed
            _mocks.MockStorage.Verify(s => s.DownloadAsync(_testMessage.StoragePath, It.IsAny<CancellationToken>()), Times.Once);
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                It.Is<DocumentUploadedMessage>(msg => msg.Metadata == ExpectedOcrResult),
                It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.IsTrue(_service.IsCleanupCalled, "Cleanup should be called");
        }

        /// <summary>
        /// This tests the Elasticsearch integration in the worker service
        /// </summary>
        [Test]
        public async Task ProcessDocumentForOcrAsync_Success_IndexesInElasticsearch()
        {
            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert - Verify Elasticsearch was called with correct parameters
            _mocks.MockElasticsearch.Verify(es => es.IndexDocumentAsync(
                _testMessage.DocumentId,
                ExpectedOcrResult,
                _testMessage.FileName,
                It.IsAny<CancellationToken>()),
                Times.Once,
                "Elasticsearch IndexDocumentAsync must be called with OCR result");
        }

        /// <summary>
        /// TEST 3: Verify processing continues even if Elasticsearch fails
        /// </summary>
        [Test]
        public async Task ProcessDocumentForOcrAsync_ElasticsearchFails_StillPublishesMessage()
        {
            // Arrange - Make Elasticsearch fail
            _mocks.MockElasticsearch.Setup(es => es.IndexDocumentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert - Message should still be published
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                It.IsAny<DocumentUploadedMessage>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        /// <summary>
        /// TEST 4: Verify OCR content is correctly captured and indexed
        /// </summary>
        [Test]
        public async Task ProcessDocumentForOcrAsync_CapturesCorrectContent()
        {
            // Arrange
            string? capturedContent = null;
            _mocks.MockElasticsearch.Setup(es => es.IndexDocumentAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string, string, CancellationToken>((id, content, filename, ct) => capturedContent = content)
                .ReturnsAsync(true);

            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert
            Assert.That(capturedContent, Is.EqualTo(ExpectedOcrResult));
            Assert.That(capturedContent, Does.Contain("Invoice"));
        }
    }
}
