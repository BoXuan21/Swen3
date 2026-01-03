namespace Swen3.API.DAL.Models
{
    public class Priority
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;  // "Very Important", "Important", "Not Very Important"
        public int Level { get; set; }              // 3, 2, 1 for sorting

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}

