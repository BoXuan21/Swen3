using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace Swen3.Storage.MiniIo;

public class MinioDocumentStorageService : IDocumentStorageService
{
    private const int MaxUploadRetries = 3;
    private const int MaxDownloadRetries = 3;

    private readonly ILogger<MinioDocumentStorageService> _logger;
    private readonly MinioOptions _options;
    private readonly IMinioClient _client;
    private readonly Regex _safeNameRegex = new(@"[^a-zA-Z0-9_\.-]", RegexOptions.Compiled);

    public MinioDocumentStorageService(IOptions<MinioOptions> options, ILogger<MinioDocumentStorageService> logger)
    {
        _logger = logger;
        _options = options.Value;

        var accessKey = string.IsNullOrWhiteSpace(_options.AccessKey)
            ? Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY")
            : _options.AccessKey;

        var secretKey = string.IsNullOrWhiteSpace(_options.SecretKey)
            ? Environment.GetEnvironmentVariable("MINIO_SECRET_KEY")
            : _options.SecretKey;

        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("MinIO credentials must be supplied via environment variables.");
        }

        var client = new MinioClient()
            .WithEndpoint(_options.Endpoint)
            .WithCredentials(accessKey, secretKey);

        if (_options.UseSsl)
        {
            client = client.WithSSL();
        }

        _client = client.Build();
    }

    public async Task<StorageObjectInfo> UploadPdfAsync(Stream content, long size, string originalFileName, string contentType, CancellationToken cancellationToken)
    {
        if (!IsPdf(contentType, originalFileName))
        {
            throw new InvalidOperationException("Only PDF documents are supported");
        }

        var safeFileName = SanitizeFileName(originalFileName);
        var objectKey = BuildObjectKey(safeFileName);

        await EnsureBucketExistsAsync(cancellationToken);

        await ExecuteWithRetryAsync(
            MaxUploadRetries,
            async () =>
            {
                if (content.CanSeek)
                {
                    content.Position = 0;
                }

                var putArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey)
                    .WithStreamData(content)
                    .WithObjectSize(size)
                    .WithContentType("application/pdf");

                await _client.PutObjectAsync(putArgs, cancellationToken);
            },
            "upload");

        _logger.LogInformation("Uploaded object {ObjectKey} to bucket {Bucket}", objectKey, _options.BucketName);

        return new StorageObjectInfo(objectKey, safeFileName, "application/pdf", size);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken)
    {
        await EnsureBucketExistsAsync(cancellationToken);
        var buffer = new MemoryStream();

        await ExecuteWithRetryAsync(
            MaxDownloadRetries,
            async () =>
            {
                buffer.SetLength(0);
                buffer.Position = 0;

                var getArgs = new GetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectKey)
                    .WithCallbackStream(stream => stream.CopyTo(buffer));

                await _client.GetObjectAsync(getArgs, cancellationToken);
            },
            "download");

        buffer.Position = 0;
        _logger.LogInformation("Downloaded object {ObjectKey} from bucket {Bucket}", objectKey, _options.BucketName);
        return buffer;
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var bucketExists = await _client.BucketExistsAsync(existsArgs, cancellationToken);

        if (bucketExists)
        {
            return;
        }

        _logger.LogInformation("Bucket {Bucket} missing. Creating.", _options.BucketName);
        var createArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
        await _client.MakeBucketAsync(createArgs, cancellationToken);
    }

    private async Task ExecuteWithRetryAsync(int maxAttempts, Func<Task> operation, string operationName)
    {
        var delay = TimeSpan.FromMilliseconds(250);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "MinIO {Operation} attempt {Attempt} failed. Retrying in {Delay}.", operationName, attempt, delay);
                await Task.Delay(delay);
                delay *= 2;
            }
        }

        // final attempt
        await operation();
    }

    private bool IsPdf(string contentType, string fileName)
    {
        var isMimePdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
        var isExtensionPdf = Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        return isMimePdf && isExtensionPdf;
    }

    private string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "document.pdf" : fileName);
        safeName = _safeNameRegex.Replace(safeName, "_");
        return safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ? safeName : $"{safeName}.pdf";
    }

    private string BuildObjectKey(string safeFileName)
    {
        var datePrefix = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"{datePrefix}/{Guid.NewGuid():N}-{safeFileName}";
    }
}

