using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.Shared.Elasticsearch;
using Swen3.Shared.Messaging;
using Swen3.Storage.MiniIo;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentsController> _logger;
        private readonly IMessagePublisher _publisher;
        private readonly IDocumentStorageService _storage;
        private readonly IElasticsearchService _elasticsearchService;

        public DocumentsController(
            IDocumentRepository repo, 
            IMapper mapper, 
            ILogger<DocumentsController> logger, 
            IMessagePublisher publisher, 
            IDocumentStorageService storage,
            IElasticsearchService elasticsearchService)
        {
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
            _publisher = publisher;
            _storage = storage;
            _elasticsearchService = elasticsearchService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GET /api/documents - Retrieving all documents");
            var documents = await _repo.GetAllAsync();
            var documentDtos = _mapper.Map<IEnumerable<DocumentDto>>(documents);
            return Ok(documentDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("GET /api/documents/{DocumentId} - Retrieving document", id);
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
            {
                _logger.LogWarning("Document with id: {DocumentId} not found", id);
                throw new NotFoundException("Document", id);
            }

            var documentDto = _mapper.Map<DocumentDto>(document);
            return Ok(documentDto);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(
            [FromQuery] string? query,
            [FromQuery] int? priorityId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("GET /api/documents/search - Searching documents with query: {Query}, priorityId: {PriorityId}", query, priorityId);

            IReadOnlyList<Guid>? documentIds = null;

            // full text search in Elasticsearch
            if (!string.IsNullOrWhiteSpace(query))
            {
                //call the elasticsearch service to search for documents
                documentIds = await _elasticsearchService.SearchAsync(query, cancellationToken);
                _logger.LogInformation("Elasticsearch returned {Count} document IDs for query: {Query}", documentIds.Count, query);

                // If Elasticsearch returns no results for a text query, return empty
                if (documentIds.Count == 0)
                {
                    return Ok(Enumerable.Empty<DocumentDto>());
                }
            }

            // Search in PostgreSQL with optional ES results and priority filter
            var documents = await _repo.SearchAsync(documentIds, priorityId);
            var documentDtos = _mapper.Map<IEnumerable<DocumentDto>>(documents);

            return Ok(documentDtos);
        }

        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)] // 25 MB PDFs 
        public async Task<IActionResult> Create([FromForm] CreateDocumentRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("POST /api/documents - Creating new document");

            if (request == null)
            {
                _logger.LogWarning("Create document request received with null body");
                throw new ValidationException("Document data is required");
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.LogWarning("Create document request received with invalid title");
                throw new ValidationException("Title is required");
            }

            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Create document request missing PDF file");
                throw new ValidationException("A PDF file is required");
            }

            await using var fileStream = request.File.OpenReadStream();
            var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
                ? "application/pdf"
                : request.File.ContentType;

            var storageInfo = await _storage.UploadPdfAsync(
                fileStream,
                request.File.Length,
                request.File.FileName,
                contentType,
                cancellationToken);

            var doc = new Document
            {
                Title = request.Title.Trim(),
                FileName = storageInfo.OriginalFileName,
                MimeType = storageInfo.ContentType,
                Size = storageInfo.Size,
                UploadedAt = DateTime.UtcNow,
                StorageKey = storageInfo.ObjectKey
            };

            await _repo.AddAsync(doc);

            _logger.LogInformation("Successfully created document with id: {DocumentId}", doc.Id);
            var createdDto = _mapper.Map<DocumentDto>(doc);

            var message = new DocumentUploadedMessage(
                    DocumentId: doc.Id,
                    FileName: doc.FileName,
                    ContentType: doc.MimeType,
                    UploadedAtUtc: DateTime.UtcNow,
                    StoragePath: doc.StorageKey,
                    Metadata: "",
                    CorrelationId: Guid.NewGuid().ToString(),
                    TenantId: null,
                    Version: 1
            );
            _logger.LogInformation("Message created: {Message}", message);

            await _publisher.PublishDocumentUploadedAsync(message, Topology.Exchange, Topology.RoutingKey);

            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, createdDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDocumentRequest request)
        {
            _logger.LogInformation("PUT /api/documents/{DocumentId} - Updating document", id);

            if (request == null)
            {
                _logger.LogWarning("Update document request received with null body");
                throw new ValidationException("Document data is required");
            }

            //find the document in the database
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
            {
                _logger.LogWarning("Document with id: {DocumentId} not found for update", id);
                throw new NotFoundException("Document", id);
            }

            // Update fields
            document.Title = request.Title.Trim();
            document.PriorityId = request.PriorityId;

            await _repo.UpdateAsync(document);

            // Re-fetch to get the updated Priority navigation property (Priority relationship)
            document = await _repo.GetByIdAsync(id);
            
            _logger.LogInformation("Successfully updated document with id: {DocumentId}", id);
            var updatedDto = _mapper.Map<DocumentDto>(document);

            return Ok(updatedDto);
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("GET /api/documents/{DocumentId}/content - Downloading document", id);
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
            {
                throw new NotFoundException("Document", id);
            }

            var contentStream = await _storage.DownloadAsync(document.StorageKey, cancellationToken);
            return File(contentStream, document.MimeType, document.FileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("DELETE /api/documents/{DocumentId} - Deleting document", id);
            
            // Get document to retrieve storage key
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
            {
                throw new NotFoundException("Document", id);
            }

            // Delete from MinIO first
            await _storage.DeleteAsync(document.StorageKey, cancellationToken);
            
            // Then delete from database
            await _repo.DeleteAsync(id);
            
            _logger.LogInformation("Successfully deleted document with id: {DocumentId} from both storage and database", id);
            return NoContent();
        }
    }
}
