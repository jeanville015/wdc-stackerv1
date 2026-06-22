using Microsoft.AspNetCore.Mvc;
using WDC_STACKER.API.Aggregate;
using WDC_STACKER.API.Models.Stacker;

namespace WDC_STACKER.API.Controllers.Stacker
{
    [ApiController]
    [Route("api/stacker")]
    public class StackerController : ControllerBase
    {
        private readonly ILogger<StackerController> _logger;
        private readonly StackerAggregate _aggregate;

        public StackerController(
            ILogger<StackerController> logger,
            StackerAggregate aggregate)
        {
            _logger = logger;
            _aggregate = aggregate;
        }

        public class ScanHolderRequest
        {
            public string Holder { get; set; } = string.Empty;
        }

        [HttpPost("scan")]
        public async Task<IActionResult> Scan([FromBody] ScanHolderRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Holder))
                return BadRequest(new { message = "Holder is required." });

            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { message = "Bearer token is required." });

            _logger.LogInformation(
                "ScanHolderJob triggered for Holder={Holder}",
                request.Holder);

            var result = await _aggregate.ScanHolderJobAsync(request.Holder, token);

            if (!result.Success &&
                result.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { result.Message });
            }

            return Ok(result);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignHolderRequest request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Holder))
                return BadRequest(new { message = "Holder is required." });

            if (string.IsNullOrWhiteSpace(request.BoxNo))
                return BadRequest(new { message = "BoxNo is required." });

            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { message = "Bearer token is required." });

            _logger.LogInformation(
                "Assign triggered for Holder={Holder}, BoxNo={BoxNo}",
                request.Holder,
                request.BoxNo);

            var result = await _aggregate.AssignHolderAsync(request, token);

            if (!result.Success &&
                result.Message.Contains("token", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { result.Message });
            }

            return Ok(result);
        }

        private static string GetBearerToken(HttpRequest request)
        {
            var authorization = request.Headers["Authorization"].ToString();

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return authorization["Bearer ".Length..].Trim();

            return string.Empty;
        }
         
        [HttpGet("boxes/{boxName}/assignments")]
        public async Task<IActionResult> GetBoxAssignments(string boxName)
        {

            var token = GetBearerToken(Request); 
            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (string.IsNullOrWhiteSpace(boxName))
                return BadRequest(new { message = "BoxName is required." });

            var assignments = await _aggregate.GetBoxAssignmentsAsync(boxName.Trim());

            return Ok(assignments);
        }
         
        [HttpDelete("assignments")]
        public async Task<IActionResult> Disassociate( [FromBody] DisassociateHolderRequest request)
        {
            var token = GetBearerToken(Request);
            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Bearer token is required." });
            }

            var result = await _aggregate.DisassociateHolderAsync(request.Holder.Trim(), token);

            if (!result.Success)
                return Conflict(new { message = result.Message });

            return Ok(new
            {
                result.Success,
                result.Message,
                GridViewBoxes = result.Boxes
            });
        }

    } 
}