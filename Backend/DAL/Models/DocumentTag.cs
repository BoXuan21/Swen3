namespace Swen3.API.DAL.Models
{
    public class DocumentTag
    {
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;

        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
