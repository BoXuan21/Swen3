using Moq;
using Tesseract;
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

        // Mocked implementation: Simulates the ImageMagick conversion process
        protected override void ConvertPdfToTiffWithImageMagick(string inputPath, string outputPath)
        {
            // Execute action defined by the test, which might throw an exception
            ConvertAction?.Invoke();

            // Simulate creation of the output file for the next step (RunOcr) to find it
            // Note: This relies on using the same temp path across the test run, which NUnit handles.
            if (!IsCleanupCalled)
            {
                // Create a dummy file to prevent a FileNotFoundException in RunOcrWithTesseract
                File.WriteAllText(outputPath, "Mock TIFF Content");
            }
        }

        // Mocked implementation: Simulates the Tesseract OCR process
        protected override string RunOcrWithTesseract(string imagePath)
        {
            // Execute function defined by the test, returning the mock result or throwing an exception
            return OcrFunc();
        }

        // Mocked implementation: Prevents actual file system deletion
        protected override void DeleteFileIfExists(string path)
        {
            IsCleanupCalled = true;
            return;
        }

        // Mocked implementation: Allows us to track that the file was written to disk
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
            // Initialize dependencies for each test
            _mocks = new OcrServiceMocks();
            // Using Mock<ILogger> is standard practice for testing components that use logging.
            var logger = new Mock<ILogger<TesseractOcrService>>();

            // Use the Testable subclass for the SUT
            _service = new TestableTesseractOcrService(
                logger.Object,
                _mocks.MockStorage.Object,
                _mocks.MockPublisher.Object
            );

            // Define the test message payload (immutable record)
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

            // Default setup for success
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

        [Test]
        public async Task ProcessDocumentForOcrAsync_WhenMinioDownloadFails_LogsFatalErrorAndAborts()
        {
            // Arrange
            _mocks.MockStorage.Setup(s => s.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Minio.Exceptions.MinioException("Network failed."));

            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert
            // 1. Verify Publisher was NEVER called
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                It.IsAny<DocumentUploadedMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never, "Result must not be published if download fails.");

            // 2. Verify Cleanup was still called (important for file stream disposal in DownloadAndSavePdf)
            Assert.IsTrue(_service.IsCleanupCalled, "Cleanup must still be called even if the download fails.");

            // 3. Verify Logger recorded the fatal error
            VerifyLog(LogLevel.Error, "FATAL Job Failure", Times.Once(), typeof(InvalidOperationException));
        }

        [Test]
        public async Task ProcessDocumentForOcrAsync_WhenImageMagickConversionFails_LogsFatalErrorAndAborts()
        {
            // Arrange
            // Configure the mock action to throw an exception during the conversion step
            _service.ConvertAction = () => throw new InvalidOperationException("ImageMagick failed to execute.");

            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert
            // 1. Verify OCR was NOT attempted (by checking publisher)
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                It.IsAny<DocumentUploadedMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never, "Result must not be published if conversion fails.");

            // 2. Verify Cleanup was called
            Assert.IsTrue(_service.IsCleanupCalled, "Cleanup must be called after conversion failure.");

            // 3. Verify a fatal error was logged (the catch block in ProcessDocumentForOcrAsync catches generic exceptions)
            VerifyLog(LogLevel.Error, "FATAL Job Failure", Times.Once(), typeof(InvalidOperationException));
        }

        [Test]
        public async Task ProcessDocumentForOcrAsync_WhenTesseractFails_LogsErrorAndAbortsPublishing()
        {
            // Arrange
            // Configure the mock function to throw a Tesseract-specific exception
            _service.OcrFunc = () => throw new TesseractException("Tessdata not found.");

            // Act
            await _service.ProcessDocumentForOcrAsync(_testMessage, CancellationToken.None);

            // Assert
            // 1. Verify Publishing was prevented
            _mocks.MockPublisher.Verify(p => p.PublishDocumentUploadedAsync(
                It.IsAny<DocumentUploadedMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>()), Times.Never, "Result must not be published if OCR fails.");

            // 2. Verify Cleanup was called
            Assert.IsTrue(_service.IsCleanupCalled, "Cleanup must be called after Tesseract failure.");

            // 3. Verify a Tesseract-specific error was logged 
            VerifyLog(LogLevel.Error, "FATAL Job Failure", Times.Once(), typeof(InvalidOperationException));
        }
    }
}
