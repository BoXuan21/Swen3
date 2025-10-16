using Microsoft.AspNetCore.Mvc;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;
using Swen3.API.DAL.Repositories;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentRepository _repo;

        public DocumentsController(DocumentRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var documents = await _repo.GetAllAsync();
            return Ok(documents.Select(d => d.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var document = await _repo.GetByIdAsync(id);
            if (document == null)
                return NotFound();

            return Ok(document.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDto dto)
        {
            // Use the system user ID for documents without specific uploaders
            var systemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            
            var doc = new Document
            {
                Title = dto.Title,
                FileName = dto.FileName,
                MimeType = dto.MimeType,
                Size = dto.Size
                // UploadedById is now optional and will be null
            };

            await _repo.AddAsync(doc);
            return CreatedAtAction(nameof(GetById), new { id = doc.Id }, doc.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
