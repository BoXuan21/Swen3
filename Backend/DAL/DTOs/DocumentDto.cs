using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.DTOs
{
    public record DocumentDto(Guid Id, string Title, string FileName, string MimeType, long Size, DateTime UploadedAt, IEnumerable<string> Tags);
    public static class DtoExtensions
    {
        public static DocumentDto ToDto(this Document d)
        {
            return new DocumentDto(d.Id, d.Title, d.FileName, d.MimeType, d.Size, d.UploadedAt, d.DocumentTags.Select(dt => dt.Tag.Name));
        }
    }
}
