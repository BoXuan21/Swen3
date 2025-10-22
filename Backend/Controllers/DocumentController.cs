using AutoMapper;
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
        private readonly IMapper _mapper;

        public DocumentsController(DocumentRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
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
