using Microsoft.AspNetCore.Mvc;
using Swen3.Gemini.Services;

namespace Swen3.Gemini.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GeminiController(GeminiService service) : ControllerBase
    {
        [HttpPost("summarize")]
        public async Task<IActionResult> Summarize([FromBody] string req)
        {
            var result = await service.SendPromptAsync(req);
            return Ok(result);
        }
    }
}
