namespace Swen3.API.DAL.Models
{
    public class Tag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = null!;
        public ICollection<DocumentTag> DocumentTags { get; set; } = new List<DocumentTag>();
    }
}
