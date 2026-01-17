namespace Swen3.API.DAL.Models
{
    public class SummaryRequest
    {
        public required string DocumentId { get; set; }
        public required string TextToSummarize { get; set; }
    }
}
