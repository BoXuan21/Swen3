namespace Swen3.Shared.Elasticsearch;

/// <summary>
/// Index model for documents stored in Elasticsearch.
/// </summary>
public class DocumentIndex
{
    /// <summary>
    /// The unique identifier of the document (matches PostgreSQL Document.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The original file name of the document.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The OCR-extracted text content from the document.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the document was indexed.
    /// </summary>
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}

