using Tesseract;
using System.Diagnostics;
using Swen3.Shared.Messaging;
using Swen3.Shared.OcrService;
using Swen3.Storage.MiniIo;

namespace Swen3.Services.OcrService
{
    public class TesseractOcrService : IOcrService
    {
        private readonly ILogger<TesseractOcrService> _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IMessagePublisher _publisher;

        private const string TessDataPath = "/usr/share/tesseract-ocr/5/tessdata";
        private const string Language = "eng";
        private const string ResultQueueName = "RESULT_QUEUE";

        // ImageMagick Configuration
        private const string ImageMagickExecutable = "convert";
        private const int PdfRenderingDpi = 300;
        private const string ImageMagickDensity = "300";
        private const string OutputFormat = "tiff";

        public TesseractOcrService(ILogger<TesseractOcrService> logger, IDocumentStorageService storage, IMessagePublisher publisher)
        {
            _logger = logger;
            _storage = storage;
            _publisher = publisher;
            _logger.LogInformation("OcrService initialized!");
        }

        public async Task ProcessDocumentForOcrAsync(DocumentUploadedMessage message, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing document {DocumentId} for OCR", message.DocumentId);

            var inputPdfPath = Path.Combine(Path.GetTempPath(), $"{message.DocumentId:N}_in.pdf");
            var outputTiffPath = Path.Combine(Path.GetTempPath(), $"{message.DocumentId:N}_out.tiff");

            String textResult;
            try
            {
                await DownloadAndSavePdf(message.StoragePath, inputPdfPath, cancellationToken);
                ConvertPdfToTiffWithImageMagick(inputPdfPath, outputTiffPath);
                textResult = RunOcrWithTesseract(outputTiffPath);
                var updatedMessage = message with
                {
                    Metadata = textResult
                };
                await _publisher.PublishDocumentUploadedAsync(updatedMessage, Topology.ResultExchange, Topology.ResultRoutingKey);
            }
            catch (TesseractException tex)
            {
                _logger.LogError(tex, "Tesseract failed processing for {Key}. Check tessdata path or image quality.", message.StoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL Job Failure for {Key}. Check MinIO access or ImageMagick installation.", message.StoragePath);
            }
            finally
            {
                DeleteFileIfExists(inputPdfPath);
                DeleteFileIfExists(outputTiffPath);
            }

        }

        protected virtual void ConvertPdfToTiffWithImageMagick(string inputPath, string outputPath)
        {
            string arguments = $"-density {ImageMagickDensity} \"{inputPath}[0]\" -compress Group4 \"{outputPath}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = ImageMagickExecutable,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException($"ImageMagick process failed to start. Is '{ImageMagickExecutable}' in PATH?");
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"ImageMagick failed (Exit Code {process.ExitCode}). Error: {error}");
            }

            if (!File.Exists(outputPath))
            {
                throw new FileNotFoundException($"ImageMagick finished but failed to create output file: {outputPath}");
            }
        }

        protected virtual async Task DownloadAndSavePdf(string objectKey, string localPath, CancellationToken cancellationToken)
        {
            if (objectKey == null || objectKey == "")
            {
                throw new NullReferenceException("Object key is empty!");
            }

            await using var pdfStream = await _storage.DownloadAsync(objectKey, cancellationToken);

            await using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await pdfStream.CopyToAsync(fileStream, cancellationToken);

            _logger.LogInformation("PDF downloaded from MinIO and saved to {Path}", localPath);
        }

        protected virtual string RunOcrWithTesseract(string imagePath)
        {
            using var pix = Tesseract.Pix.LoadFromFile(imagePath);

            if (pix == null)
            {
                throw new InvalidOperationException($"Failed to load image from path: {imagePath}. Check file format and permissions.");
            }

            using var engine = new TesseractEngine(TessDataPath, Language, EngineMode.Default);

            using var page = engine.Process(pix);

            float confidence = page.GetMeanConfidence();
            if (confidence < 0.70f)
            {
                _logger.LogWarning("Low confidence result ({Confidence:P2}) for image: {Path}", confidence, imagePath);
            }

            return page.GetText();
        }

        protected virtual void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); }
                catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temporary file: {Path}", path); }
            }
        }
    }
}
