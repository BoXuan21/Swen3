namespace Swen3.Shared.Messaging
{
    public sealed record DocumentUploadedMessage
    (
        Guid DocumentId,
        string FileName,
        string ContentType,
        DateTime UploadedAtUtc,
        string StoragePath,
        string Metadata,
        string Summary,
        string CorrelationId,
        string? TenantId,
        int Version
    );
}


