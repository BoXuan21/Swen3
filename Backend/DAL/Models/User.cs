namespace Swen3.API.DAL.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
    }
}
