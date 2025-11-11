using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.API.DAL.Repositories;
using Swen3.API.Messaging;
using System.Text;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentRepository _repo;
        private readonly IMapper _mapper;
        private readonly IMessagePublisher _publisher;

        public DocumentsController(DocumentRepository repo, IMapper mapper, IMessagePublisher publisher)
        {
            _repo = repo;
            _mapper = mapper;
            _publisher = publisher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _repo.GetAllAsync();
            var documentDtos = _mapper.Map<IEnumerable<DocumentDto>>(documents);
            return Ok(documentDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
                return NotFound();

            var documentDto = _mapper.Map<DocumentDto>(document);
            return Ok(documentDto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDto dto)
        {            
            var doc = _mapper.Map<Document>(dto);
            await _repo.AddAsync(doc);
            
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
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
