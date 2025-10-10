﻿namespace Swen3.API.DAL.Models
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public long Size { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public Guid UploadedById { get; set; }
        public User UploadedBy { get; set; } = null!;

        public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
    }
}
