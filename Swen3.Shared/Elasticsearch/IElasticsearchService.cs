namespace Swen3.Shared.Elasticsearch;

/// <summary>
/// Service interface for Elasticsearch operations.
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Indexes a document with the given OCR content in Elasticsearch.
    /// </summary>
    Task<bool> IndexDocumentAsync(Guid id, string content, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents matching the given query.
    /// </summary>
    Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from the Elasticsearch index.
    /// </summary>
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}

