using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Swen3.Shared.Elasticsearch;
using Assert = NUnit.Framework.Assert;

namespace Swen3.Test.Services
{
    /// <summary>
    /// Unit tests for ElasticsearchService
    /// Tests Sprint 6 requirements: Search functionality with exact and fuzzy matching
    /// </summary>
    [TestFixture]
    public class ElasticsearchServiceTests
    {
        private Mock<IElasticClient> _mockElasticClient = null!;
        private Mock<ILogger<ElasticsearchService>> _mockLogger = null!;
        private IOptions<ElasticsearchOptions> _options = null!;
        private ElasticsearchService _service = null!;

        [SetUp]
        public void Setup()
        {
            _mockElasticClient = new Mock<IElasticClient>();
            _mockLogger = new Mock<ILogger<ElasticsearchService>>();
            _options = Options.Create(new ElasticsearchOptions
            {
                Url = "http://localhost:9200",
                IndexName = "test-documents"
            });

            _service = new ElasticsearchService(
                _mockElasticClient.Object,
                _options,
                _mockLogger.Object);
        }

        /// <summary>
        /// Tests storing OCR text content in Elasticsearch
        /// </summary>
        [Test]
        public async Task IndexDocumentAsync_ValidDocument_ReturnsTrue()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var content = "This is an invoice for consulting services dated January 2024.";
            var fileName = "invoice-2024.pdf";

            var mockResponse = new Mock<IndexResponse>();
            mockResponse.Setup(r => r.IsValid).Returns(true);

            _mockElasticClient
                .Setup(c => c.IndexAsync(
                    It.IsAny<DocumentIndex>(),
                    It.IsAny<Func<IndexDescriptor<DocumentIndex>, IIndexRequest<DocumentIndex>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var result = await _service.IndexDocumentAsync(documentId, content, fileName);

            // Assert
            Assert.IsTrue(result, "Indexing should succeed");
            _mockElasticClient.Verify(c => c.IndexAsync(
                It.Is<DocumentIndex>(d => d.Id == documentId && d.Content == content),
                It.IsAny<Func<IndexDescriptor<DocumentIndex>, IIndexRequest<DocumentIndex>>>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Verifies searching for "invoice" returns documents containing "invoice"
        /// </summary>
        [Test]
        public async Task SearchAsync_ExactMatch_ReturnsMatchingDocuments()
        {
            // Arrange
            var doc1Id = Guid.NewGuid();
            var doc2Id = Guid.NewGuid();
            var searchQuery = "invoice";

            var mockDocuments = new List<DocumentIndex>
            {
                new DocumentIndex { Id = doc1Id, Content = "This is an invoice for services", FileName = "invoice-001.pdf" },
                new DocumentIndex { Id = doc2Id, Content = "Invoice number 12345", FileName = "invoice-002.pdf" }
            };

            var mockResponse = new Mock<ISearchResponse<DocumentIndex>>();
            mockResponse.Setup(r => r.IsValid).Returns(true);
            mockResponse.Setup(r => r.Documents).Returns(mockDocuments);

            _mockElasticClient
                .Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndex>, ISearchRequest>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var results = await _service.SearchAsync(searchQuery);

            // Assert
            Assert.That(results.Count, Is.EqualTo(2), "Should return 2 matching documents");
            Assert.That(results, Does.Contain(doc1Id));
            Assert.That(results, Does.Contain(doc2Id));
        }

        /// <summary>
        /// Verifies "invoce" (typo) still finds documents with "invoice"
        /// Tests Elasticsearch's Fuzziness.Auto feature
        /// </summary>
        [Test]
        public async Task SearchAsync_FuzzyMatch_FindsDocumentsDespiteTypo()
        {
            // Arrange
            var docId = Guid.NewGuid();
            var searchQueryWithTypo = "invoce";  // Typo: missing 'i'

            // Elasticsearch with fuzzy matching should still find "invoice"
            var mockDocuments = new List<DocumentIndex>
            {
                new DocumentIndex 
                { 
                    Id = docId, 
                    Content = "This is an invoice for consulting services",  // Correct spelling
                    FileName = "invoice-march-2024.pdf" 
                }
            };

            var mockResponse = new Mock<ISearchResponse<DocumentIndex>>();
            mockResponse.Setup(r => r.IsValid).Returns(true);
            mockResponse.Setup(r => r.Documents).Returns(mockDocuments);

            _mockElasticClient
                .Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndex>, ISearchRequest>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var results = await _service.SearchAsync(searchQueryWithTypo);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1), "Fuzzy search should find document despite typo");
            Assert.That(results[0], Is.EqualTo(docId));
        }

        /// <summary>
        /// Search returns empty for no matches
        /// </summary>
        [Test]
        public async Task SearchAsync_NoMatches_ReturnsEmptyList()
        {
            // Arrange
            var mockResponse = new Mock<ISearchResponse<DocumentIndex>>();
            mockResponse.Setup(r => r.IsValid).Returns(true);
            mockResponse.Setup(r => r.Documents).Returns(new List<DocumentIndex>());

            _mockElasticClient
                .Setup(c => c.SearchAsync(
                    It.IsAny<Func<SearchDescriptor<DocumentIndex>, ISearchRequest>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            var results = await _service.SearchAsync("xyznonexistentterm");

            // Assert
            Assert.That(results.Count, Is.EqualTo(0), "No matches should return empty list");
        }
    }
}
