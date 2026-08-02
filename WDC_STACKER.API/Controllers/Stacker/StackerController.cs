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
        public async Task<IActionResult> GetShipBoxes(
            string boxNo,
            [FromQuery] bool suggest = false,
            [FromQuery] string? lec = null)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (string.IsNullOrWhiteSpace(boxNo))
                return BadRequest(new { message = "BoxNo is required." });

            var hasLecContext = Request.Query.ContainsKey("lec");

            var shipBoxes = await _aggregate.GetShipBoxesAsync(
                boxNo.Trim(),
                suggest,
                GetClientKey(),
                lec,
                hasLecContext);

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

        [HttpGet("withdrawal/requests")]
        public async Task<IActionResult> GetFgiWithdrawalRequests()
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) ||
                !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (!string.Equals(
                    GetClientKey(),
                    "WDC_STACKER.CLIENT.FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var requests = await _aggregate.GetFgiWithdrawalRequestsAsync();
            return Ok(requests);
        }

        [HttpGet("withdrawal/disassociation-preview")]
        public async Task<IActionResult> GetFgiWithdrawalDisassociationPreview([FromQuery] string lec, [FromQuery] string? penNum, [FromQuery] int? total)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) ||
                !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(
                    new { message = "Invalid or expired token." });
            }

            if (!string.Equals(
                    GetClientKey(),
                    "WDC_STACKER.CLIENT.FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(lec))
            {
                return BadRequest(
                    new { message = "LEC is required." });
            }

            if (!total.HasValue || total.Value < 0)
            {
                return BadRequest(new
                {
                    message =
                        "TOTAL is required and cannot be negative."
                });
            }

            var result = await _aggregate.GetFgiWithdrawalDisassociationPreviewAsync(lec.Trim(), string.IsNullOrWhiteSpace(penNum) ? null : penNum.Trim(), total.Value, token, GetClientKey());

            if (!result.Success || result.Preview is null)
            {
                if (result.Message.Contains(
                        "token",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Unauthorized(
                        new { message = result.Message });
                }

                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new { message = result.Message });
            }

            return Ok(result.Preview);
        }

        [HttpPost( "withdrawal/requests/{requestId:long}/disassociate")]
        public async Task<IActionResult> DisassociateFgiWithdrawalRequest( long requestId, [FromBody] FgiWithdrawalDisassociationRequest? request)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) ||
                !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(
                    new { message = "Invalid or expired token." });
            }

            if (!string.Equals(
                    GetClientKey(),
                    "WDC_STACKER.CLIENT.FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (requestId <= 0)
            {
                return BadRequest(
                    new { message = "RequestId is required." });
            }

            if (string.IsNullOrWhiteSpace(request?.ShippingId))
            {
                return BadRequest(
                    new { message = "ShippingId is required." });
            }

            if (request?.IncludedHolders is null ||
                request.IncludedHolders.Count == 0)
            {
                return BadRequest(new
                {
                    message =
                        "At least one included Holder is required."
                });
            }

            if (request.IncludedHolders.Count > 2000)
            {
                return BadRequest(
                    new { message = "Too many included Holders." });
            }

            if (request.IncludedHolders.Any(
                    holder =>
                        string.IsNullOrWhiteSpace(holder) ||
                        holder.Trim().Length > 50))
            {
                return BadRequest(new
                {
                    message =
                        "Every Holder is required and cannot exceed 50 characters."
                });
            }

            if (request.IncludedHolders
                    .Select(holder => holder.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count() !=
                request.IncludedHolders.Count)
            {
                return BadRequest(new
                {
                    message =
                        "The included Holder list contains duplicates."
                });
            }

            var result =
                await _aggregate
                    .DisassociateFgiWithdrawalRequestAsync(
                        requestId,
                        request.ShippingId.Trim(),
                        request.IncludedHolders,
                        token);

            if (!result.Success)
            {
                return Conflict(
                    new { message = result.Message });
            }

            return Ok(result);
        }

        [HttpPatch("withdrawal/requests/{requestId:long}/acknowledge")]
        public async Task<IActionResult> AcknowledgeFgiWithdrawalRequest(long requestId)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) ||
                !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (!string.Equals(
                    GetClientKey(),
                    "WDC_STACKER.CLIENT.FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (requestId <= 0)
                return BadRequest(new { message = "RequestId is required." });

            var result =
                await _aggregate.AcknowledgeFgiWithdrawalRequestAsync(
                    requestId,
                    token);

            if (!result.Success)
                return Conflict(new { message = result.Message });

            return Ok(new
            {
                result.Success,
                result.Message,
                result.AcknowledgeBy
            });
        }

        [HttpGet("withdrawal/layout")]
        public async Task<IActionResult> GetFgiWithdrawalLayout([FromQuery] string lec)
        {
            var token = GetBearerToken(Request);

            if (string.IsNullOrWhiteSpace(token) ||
                !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            if (!string.Equals(
                    GetClientKey(),
                    "WDC_STACKER.CLIENT.FGI",
                    StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(lec))
                return BadRequest(new { message = "LEC is required." });

            var layout = await _aggregate.GetFgiWithdrawalLayoutAsync(
                lec.Trim(),
                GetClientKey());

            if (layout is null)
                return NoContent();

            return Ok(layout);
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
