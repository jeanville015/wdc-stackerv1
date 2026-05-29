using Microsoft.AspNetCore.Mvc;
using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Controllers
{
    [ApiController]
    [Route("api/feats/query")]
    public class FeatsQueryController : ControllerBase
    {
        private readonly FeatsService _featsService;

        public FeatsQueryController(FeatsService featsService)
        {
            _featsService = featsService;
        }

        [HttpPost]
        public async Task<IActionResult> Query([FromBody] FeatsQueryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.QueryType))
                return BadRequest(new { message = "QueryType is required." });

            if (request.RecordLimit <= 0)
                return BadRequest(new { message = "RecordLimit must be greater than zero." });

            var username = Request.Headers["X-Feats-Username"].ToString();
            var password = Request.Headers["X-Feats-Password"].ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Unauthorized(new { message = "FEATS credentials are required." });

            var result = await _featsService.QueryAsync(request, username, password);

            if (!result.Success)
                return StatusCode(502, new { result.Message });

            return Ok(result);
        }
    }
}