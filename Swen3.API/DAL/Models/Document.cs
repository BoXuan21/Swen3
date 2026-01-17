namespace Swen3.API.DAL.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string Metadata { get; set; } = "";
        public string StorageKey { get; set; } = null!;
        public string Summary { get; set; } = "";

        // Priority relationship
        public int? PriorityId { get; set; } //allows a document to not have a priority
        public Priority? Priority { get; set; }
    }
}
