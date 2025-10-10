using Microsoft.AspNetCore.Mvc;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;

        public DocumentsController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // GET: api/documents
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _uow.Documents.ListAsync();
            return Ok(documents.Select(d => d.ToDto()));
        }

        // GET: api/documents/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _uow.Documents.GetWithTagsAsync(id);
            if (document == null)
                return NotFound();

            return Ok(document.ToDto());
        }

        // POST: api/documents
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDto dto)
        {
            var doc = new Document
            {
                Title = dto.Title,
                FileName = dto.FileName,
                MimeType = dto.MimeType,
                Size = dto.Size,
                UploadedById = Guid.NewGuid() // placeholder until user system is ready
            };

            await _uow.Documents.AddAsync(doc);
            await _uow.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc.ToDto());
        }

        // DELETE: api/documents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var document = await _uow.Documents.GetByIdAsync(id);
            if (document == null)
                return NotFound();

            _uow.Documents.Remove(document);
            await _uow.SaveChangesAsync();

            return NoContent();
        }
    }
}
