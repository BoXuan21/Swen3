using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace Swen3.Shared.Elasticsearch;

/// <summary>
/// Elasticsearch service implementation using the NEST client.
/// </summary>
public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _client;
    private readonly ElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(IOptions<ElasticsearchOptions> options, ILogger<ElasticsearchService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var settings = new ConnectionSettings(new Uri(_options.Url))
            .DefaultIndex(_options.IndexName)
            .EnableDebugMode();

        _client = new ElasticClient(settings);
    }

    /// <summary>
    /// Constructor for testing with injected client.
    /// </summary>
    public ElasticsearchService(IElasticClient client, IOptions<ElasticsearchOptions> options, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IndexDocumentAsync(Guid id, string content, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var document = new DocumentIndex
            {
                Id = id,
                Content = content,
                FileName = fileName,
                IndexedAt = DateTime.UtcNow
            };

            var response = await _client.IndexAsync(document, idx => idx
                .Index(_options.IndexName)
                .Id(id)
                .Refresh(Refresh.True), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index document {DocumentId}: {Error}", id, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully indexed document {DocumentId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing document {DocumentId}", id);
            return false;
        }
    }

    public async Task<IReadOnlyList<Guid>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<Guid>();
            }

            var response = await _client.SearchAsync<DocumentIndex>(s => s
                .Index(_options.IndexName)
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(f => f
                            .Field(doc => doc.Content)
                            .Field(doc => doc.FileName))
                        .Query(query)
                        .Fuzziness(Fuzziness.Auto)
                    )
                )
                .Size(100), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Search failed: {Error}", response.DebugInformation);
                return Array.Empty<Guid>();
            }

            var documentIds = response.Documents.Select(d => d.Id).ToList();
            _logger.LogInformation("Search for '{Query}' returned {Count} results", query, documentIds.Count);
            return documentIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for '{Query}'", query);
            return Array.Empty<Guid>();
        }
    }

    public async Task<bool> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.DeleteAsync<DocumentIndex>(id, d => d
                .Index(_options.IndexName)
                .Refresh(Refresh.True), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete document {DocumentId}: {Error}", id, response.DebugInformation);
                return false;
            }

            _logger.LogInformation("Successfully deleted document {DocumentId} from index", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return false;
        }
    }
}

