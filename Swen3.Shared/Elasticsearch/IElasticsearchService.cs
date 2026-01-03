namespace Swen3.Shared.Elasticsearch;

/// <summary>
/// Service interface for Elasticsearch operations.
/// </summary>
public interface IElasticsearchService
{
    /// <summary>
    /// Indexes a document with the given OCR content in Elasticsearch.
    /// </summary>
    /// <param name="id">The unique identifier of the document.</param>
    /// <param name="content">The OCR-extracted text content.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if indexing succeeded, false otherwise.</returns>
    Task<bool> IndexDocumentAsync(Guid id, string content, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for documents matching the given query.
    /// </summary>
    /// <param name="query">The search query string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of document IDs that match the query.</returns>
    Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document from the Elasticsearch index.
    /// </summary>
    /// <param name="id">The unique identifier of the document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deletion succeeded, false otherwise.</returns>
    Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default);
}

