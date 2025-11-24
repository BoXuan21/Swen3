using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.API.Messaging;
using Swen3.Shared.Messaging;

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

        public DocumentsController(IDocumentRepository repo, IMapper mapper, ILogger<DocumentsController> logger, IMessagePublisher publisher)
        {
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
            _publisher = publisher;
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
        public async Task<IActionResult> Create([FromBody] DocumentDto dto)
        {
            _logger.LogInformation("POST /api/documents - Creating new document");
            
            if (dto == null)
            {
                _logger.LogWarning("Create document request received with null body");
                throw new ValidationException("Document data is required");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                _logger.LogWarning("Create document request received with invalid title");
                throw new ValidationException("Title is required");
            }

            var doc = _mapper.Map<Document>(dto);
            await _repo.AddAsync(doc);
            
            _logger.LogInformation("Successfully created document with id: {DocumentId}", doc.Id);
            var createdDto = _mapper.Map<DocumentDto>(doc);

            var message = new DocumentUploadedMessage(
                    DocumentId: doc.Id,
                    FileName: doc.FileName,
                    ContentType: doc.MimeType,
                    UploadedAtUtc: DateTime.UtcNow,
                    StoragePath: "",
                    CorrelationId: Guid.NewGuid().ToString(),
                    TenantId: null,
                    Version: 1
                );

            await _publisher.PublishDocumentUploadedAsync(message);

            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, createdDto);
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
