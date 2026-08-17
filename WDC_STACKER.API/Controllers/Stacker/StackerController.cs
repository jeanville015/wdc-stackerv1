using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
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
                hasLecContext,
                token);

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
        public async Task<IActionResult> GetFgiWithdrawalDisassociationPreview([FromQuery] string? lec, [FromQuery] string? penNum, [FromQuery] int? total, [FromQuery] string? partNum, [FromQuery] string? grade, [FromQuery] int? actualOutput)
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

            if (!total.HasValue || total.Value < 0)
            {
                return BadRequest(new
                {
                    message =
                        "TOTAL is required and cannot be negative."
                });
            }

            if (string.IsNullOrWhiteSpace(partNum))
            {
                return BadRequest(new
                {
                    message =
                        "PartNum is required."
                });
            }

            if (string.IsNullOrWhiteSpace(grade))
            {
                return BadRequest(new
                {
                    message =
                        "Grade is required."
                });
            }

            var result = await _aggregate.GetFgiWithdrawalDisassociationPreviewAsync(string.IsNullOrWhiteSpace(lec) ? null : lec.Trim(), string.IsNullOrWhiteSpace(penNum) ? null : penNum.Trim(), total.Value, string.IsNullOrWhiteSpace(partNum) ? null : partNum.Trim(), string.IsNullOrWhiteSpace(grade) ? null : grade.Trim(), actualOutput ?? 0, token, GetClientKey());

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

        [HttpGet("withdrawal/verify-shipbox")]
        public async Task<IActionResult> VerifyFgiWithdrawalShipBox([FromQuery] string? shippingId)
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

            if (string.IsNullOrWhiteSpace(shippingId))
            {
                return BadRequest(
                    new { message = "ShippingId is required." });
            }

            var result = await _aggregate.VerifyFgiWithdrawalShipBoxAsync(shippingId.Trim(), token);

            if (!result.Success)
            {
                return Conflict(new { message = result.Message });
            }

            return Ok(new { message = result.Message, camVersion = result.CamVersion });
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

            if (string.IsNullOrWhiteSpace(request.ShippingId))
            {
                return BadRequest(
                    new { message = "ShippingId is required." });
            }

            var result =
                await _aggregate
                    .DisassociateFgiWithdrawalRequestAsync(
                        requestId,
                        request.IncludedHolders,
                        request.ShippingId,
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
        public async Task<IActionResult> GetFgiWithdrawalLayout([FromQuery] string? lec, [FromQuery] string? penNum, [FromQuery] string? partNum, [FromQuery] string? grade)
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

            var layout = await _aggregate.GetFgiWithdrawalLayoutAsync(
                string.IsNullOrWhiteSpace(lec) ? null : lec.Trim(),
                string.IsNullOrWhiteSpace(penNum) ? null : penNum.Trim(),
                string.IsNullOrWhiteSpace(partNum) ? null : partNum.Trim(),
                string.IsNullOrWhiteSpace(grade) ? null : grade.Trim(),
                GetClientKey(),
                token);

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

        [HttpDelete("fgi/hold-assignments")]
        public async Task<IActionResult> DisassociateFgiHolder([FromBody] DisassociateHolderRequest request)
        {
            var token = GetBearerToken(Request);
            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Bearer token is required." });
            }

            var result = await _aggregate.DisassociateFgiHolderAsync(request.Holder.Trim(), token, GetClientKey());

            if (!result.Success)
                return Conflict(new { message = result.Message });

            return Ok(new
            {
                result.Success,
                result.Message,
                GridViewBoxes = result.Boxes
            });
        }

        [HttpGet("boxes")]
        public async Task<IActionResult> GetBoxes()
        {
            var token = GetBearerToken(Request);
            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            var result = await _aggregate.MapGridViewBoxData(GetClientKey(), null, token);

            return Ok(new
            {
                Success = true,
                CanAssign = false,
                Message = result.Message,
                GridViewBoxes = result.Boxes
            });
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv()
        {
            var token = GetBearerToken(Request);
            if (string.IsNullOrWhiteSpace(token) || !_aggregate.IsSessionTokenValid(token))
            {
                return Unauthorized(new { message = "Invalid or expired token." });
            }

            var clientKey = GetClientKey();
            var data = await _aggregate.GetAllHolderAssignmentsForCsvAsync(clientKey);

            var csv = GenerateCsv(data);

            // Add UTF-8 BOM for proper Excel encoding recognition
            var bom = System.Text.Encoding.UTF8.GetPreamble();
            var csvBytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var csvWithBom = new byte[bom.Length + csvBytes.Length];
            Buffer.BlockCopy(bom, 0, csvWithBom, 0, bom.Length);
            Buffer.BlockCopy(csvBytes, 0, csvWithBom, bom.Length, csvBytes.Length);

            var isFgi = string.Equals(clientKey, "WDC_STACKER.CLIENT.FGI", StringComparison.OrdinalIgnoreCase);
            var timestamp = DateTime.Now.ToString("MMM-dd-yyyy_HHmm");
            var filename = $"{timestamp}_{(isFgi ? "FGI" : "PWD")}-Stacker_WIP.csv";
            Response.Headers.Clear();
            Response.Headers.Append("Content-Type", "text/csv; charset=utf-8");
            Response.Headers.Append("Content-Disposition", $"attachment; filename={filename}");
            return File(csvWithBom, "text/csv; charset=utf-8");
        }

        private string GenerateCsv(List<WDC_STACKER.API.Models.Stacker.CsvExportRow> data)
        {
            var headers = new[] { "Holder", "Job", "Qty", "Grade", "Position", "InsertedOn", "Quantity", "Model", "PartNum", "PenNum", "Lec", "Status" };
            var csv = new System.Text.StringBuilder();

            csv.AppendLine(string.Join(",", headers));

            foreach (var row in data)
            {
                var values = new[]
                {
                    EscapeCsvField(row.Holder),
                    EscapeCsvField(row.Job),
                    row.Qty.ToString(),
                    EscapeCsvField(row.Grade),
                    EscapeCsvField(FormatPosition(row.BlackBox, row.ShipBox)),
                    EscapeCsvField(row.InsertedOn),
                    row.Quantity.ToString(),
                    EscapeCsvField(row.Model),
                    EscapeCsvField(row.PartNum),
                    EscapeCsvField(row.PenNum),
                    EscapeCsvField(row.Lec),
                    EscapeCsvField(row.Status)
                };
                csv.AppendLine(string.Join(",", values));
            }

            return csv.ToString();
        }

        private static readonly Regex BlackBoxPositionPattern =
            new(@"R(\d+)L\d+C(\d+)", RegexOptions.Compiled);
        private static readonly Regex ShipBoxPositionPattern =
            new(@"S\d+L\d+C(\d+)", RegexOptions.Compiled);

        /// <summary>
        /// Formats the raw BlackBox (e.g. "R01L02C01") and ShipBox (e.g. "S01L01C01") names
        /// into a simplified "R01-B01-S01" position string, matching the client display naming.
        /// </summary>
        private string FormatPosition(string blackBox, string shipBox)
        {
            var parts = new List<string>();

            var boxMatch = BlackBoxPositionPattern.Match(blackBox ?? string.Empty);
            if (boxMatch.Success)
            {
                parts.Add($"R{int.Parse(boxMatch.Groups[1].Value):00}");
                parts.Add($"B{int.Parse(boxMatch.Groups[2].Value):00}");
            }

            var shipMatch = ShipBoxPositionPattern.Match(shipBox ?? string.Empty);
            if (shipMatch.Success)
            {
                parts.Add($"S{int.Parse(shipMatch.Groups[1].Value):00}");
            }

            return parts.Count > 0
                ? string.Join("-", parts)
                : $"{blackBox} {shipBox}".Trim();
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

    } 
}
