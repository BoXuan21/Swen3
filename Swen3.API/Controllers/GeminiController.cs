using Microsoft.AspNetCore.Mvc;
using Swen3.Gemini.Services;
using Swen3.API.DAL.Interfaces;
using System.Text.Json.Nodes;

namespace Swen3.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController : ControllerBase
    {
        private readonly ILogger<GeminiController> _logger;
        private readonly IDocumentRepository _repo;
        private readonly IGeminiService _service;

        public GeminiController(ILogger<GeminiController> logger, IDocumentRepository repo, IGeminiService service)
        {
            _logger = logger;
            _repo = repo;
            _service = service;
        }

        [HttpPost("summarize")]
        public async Task<IActionResult> Summarize([FromBody] SummaryRequest request)
        {
            var result = await _service.SendPromptAsync(request.TextToSummarize);
            var resultNode = JsonNode.Parse(result);
            string summaryText = resultNode["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();
            await _repo.UpdateSummaryAsync(Guid.Parse(request.DocumentId), summaryText);
            return Ok(summaryText);
        }
    }
}
