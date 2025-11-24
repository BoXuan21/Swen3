using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.API.Messaging;
using Swen3.API.Storage;

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

        public DocumentsController(IDocumentRepository repo, IMapper mapper, ILogger<DocumentsController> logger, IMessagePublisher publisher, IDocumentStorageService storage)
        {
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
            _publisher = publisher;
            _storage = storage;
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

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25 * 1024 * 1024)] // 25 MB PDFs by default
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
                    CorrelationId: Guid.NewGuid().ToString(),
                    TenantId: null,
                    Version: 1
                );

            await _publisher.PublishDocumentUploadedAsync(message);

            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, createdDto);
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
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("DELETE /api/documents/{DocumentId} - Deleting document", id);
            await _repo.DeleteAsync(id);
            _logger.LogInformation("Successfully deleted document with id: {DocumentId}", id);
            return NoContent();
        }
    }
}
