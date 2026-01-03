using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Swen3.API.Common.Exceptions;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Interfaces;
using Swen3.API.DAL.Models;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrioritiesController : ControllerBase
    {
        private readonly IPriorityRepository _repo;
        private readonly IMapper _mapper;
        private readonly ILogger<PrioritiesController> _logger;

        public PrioritiesController(IPriorityRepository repo, IMapper mapper, ILogger<PrioritiesController> logger)
        {
            _repo = repo;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("GET /api/priorities - Retrieving all priorities");
            var priorities = await _repo.GetAllAsync();
            var priorityDtos = _mapper.Map<IEnumerable<PriorityDto>>(priorities);
            return Ok(priorityDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("GET /api/priorities/{PriorityId} - Retrieving priority", id);
            var priority = await _repo.GetByIdAsync(id);
            if (priority == null)
            {
                _logger.LogWarning("Priority with id: {PriorityId} not found", id);
                throw new NotFoundException("Priority", id);
            }

            var priorityDto = _mapper.Map<PriorityDto>(priority);
            return Ok(priorityDto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePriorityDto request)
        {
            _logger.LogInformation("POST /api/priorities - Creating new priority");

            if (request == null)
            {
                _logger.LogWarning("Create priority request received with null body");
                throw new ValidationException("Priority data is required");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Create priority request received with invalid name");
                throw new ValidationException("Name is required");
            }

            var priority = new Priority
            {
                Name = request.Name.Trim(),
                Level = request.Level
            };

            await _repo.AddAsync(priority);

            _logger.LogInformation("Successfully created priority with id: {PriorityId}", priority.Id);
            var createdDto = _mapper.Map<PriorityDto>(priority);

            return CreatedAtAction(nameof(GetById), new { id = priority.Id }, createdDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePriorityDto request)
        {
            _logger.LogInformation("PUT /api/priorities/{PriorityId} - Updating priority", id);

            if (request == null)
            {
                _logger.LogWarning("Update priority request received with null body");
                throw new ValidationException("Priority data is required");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                _logger.LogWarning("Update priority request received with invalid name");
                throw new ValidationException("Name is required");
            }

            var priority = await _repo.GetByIdAsync(id);
            if (priority == null)
            {
                _logger.LogWarning("Priority with id: {PriorityId} not found for update", id);
                throw new NotFoundException("Priority", id);
            }

            priority.Name = request.Name.Trim();
            priority.Level = request.Level;

            await _repo.UpdateAsync(priority);

            _logger.LogInformation("Successfully updated priority with id: {PriorityId}", priority.Id);
            var updatedDto = _mapper.Map<PriorityDto>(priority);

            return Ok(updatedDto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("DELETE /api/priorities/{PriorityId} - Deleting priority", id);
            await _repo.DeleteAsync(id);
            _logger.LogInformation("Successfully deleted priority with id: {PriorityId}", id);
            return NoContent();
        }
    }
}

