namespace Swen3.API.DAL.Models
{
    /// <summary>
    /// Stores daily access statistics per document from external systems
    /// </summary>
    public class DocumentAccessLog
    {
        public int Id { get; set; }
        
        /// <summary>
        /// Reference to the document being accessed
        /// </summary>
        public Guid DocumentId { get; set; }
        public Document Document { get; set; } = null!;
        
        /// <summary>
        /// The date of the access (time component is ignored)
        /// </summary>
        public DateTime AccessDate { get; set; }
        
        /// <summary>
        /// Total number of accesses on this date
        /// </summary>
        public int AccessCount { get; set; }
        
        /// <summary>
        /// Comma-separated list of external systems that reported accesses
        /// </summary>
        public string Sources { get; set; } = "";
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }
}

