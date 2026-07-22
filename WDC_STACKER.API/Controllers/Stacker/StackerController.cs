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
        private string GetClientKey()
        {
            var clientKey = Request.Headers["X-Stacker-Client"].ToString();
            return string.IsNullOrWhiteSpace(clientKey)
                ? "WDC_STACKER.CLIENT.PWD"
                : clientKey;
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

            var result = await _aggregate.ScanHolderJobAsync(request.Holder, token, GetClientKey());

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

            var clientKey = GetClientKey();
            var isFgi = string.Equals(clientKey, "WDC_STACKER.CLIENT.FGI", StringComparison.OrdinalIgnoreCase);

            if (isFgi && string.IsNullOrWhiteSpace(request.ShipBoxName))
                return BadRequest(new { message = "ShipBoxName is required for FGI." });

            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { message = "Bearer token is required." });

            _logger.LogInformation(
                "Assign triggered for Holder={Holder}, BoxNo={BoxNo}, ShipBoxName={ShipBoxName}",
                request.Holder,
                request.BoxNo,
                request.ShipBoxName);

            var result = await _aggregate.AssignHolderAsync(request, token, clientKey);

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

        [HttpGet("boxes/{boxNo}/shipboxes")]
        public async Task<IActionResult> GetShipBoxes(string boxNo, [FromQuery] bool suggest = false)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (string.IsNullOrWhiteSpace(boxNo))
                return BadRequest(new { message = "BoxNo is required." });

            var shipBoxes = await _aggregate.GetShipBoxesAsync(boxNo.Trim(), suggest, GetClientKey());

            return Ok(shipBoxes);
        }

        [HttpGet("boxes/{boxName}/shipboxes/{shipBoxName}/assignments")]
        public async Task<IActionResult> GetShipBoxAssignments(string boxName, string shipBoxName)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (string.IsNullOrWhiteSpace(shipBoxName))
                return BadRequest(new { message = "ShipBoxName is required." });

            var assignments = await _aggregate.GetShipBoxAssignmentsAsync(boxName.Trim(), shipBoxName.Trim(),GetClientKey());

            return Ok(assignments);
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

            var assignments = await _aggregate.GetBoxAssignmentsAsync(boxName.Trim(), GetClientKey());

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

            var result = await _aggregate.DisassociateHolderAsync(request.Holder.Trim(), token, GetClientKey());

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