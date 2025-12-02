namespace Swen3.Storage.MiniIo;

public interface IDocumentStorageService
{
    Task<StorageObjectInfo> UploadPdfAsync(Stream content, long size, string originalFileName, string contentType, CancellationToken cancellationToken);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

