namespace Swen3.API.DAL.DTOs
{
    public record DocumentDto(Guid Id, string Title, string FileName, string MimeType, long Size, DateTime UploadedAt, string Metadata, string StorageKey, string Summary, int? PriorityId, string? PriorityName);
}
