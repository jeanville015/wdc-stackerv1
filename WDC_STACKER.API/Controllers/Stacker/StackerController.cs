using Microsoft.AspNetCore.Mvc;

namespace WDC_STACKER.API.Controllers.Stacker
{
    [ApiController]
    [Route("api/stacker")]
    public class StackerController : ControllerBase
    {
        private readonly ILogger<StackerController> _logger;

        public StackerController(ILogger<StackerController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Receives a scanned ID from the scan-holder textbox.
        /// POST /api/stacker/scan
        /// Body: { "scannedId": "..." }
        /// </summary>
        [HttpPost("scan")]
        public IActionResult Scan([FromBody] ScanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ScannedId))
                return BadRequest(new { message = "ScannedId is required." });

            _logger.LogInformation("Scan triggered with ID={ScannedId}", request.ScannedId);

            // TODO: wire to Aggregate / Service layer when logic is defined
            return Ok(new
            {
                success = true,
                scannedId = request.ScannedId,
                message = $"Scanned ID '{request.ScannedId}' received."
            });
        }

        /// <summary>
        /// Triggers the assign procedure. No parameter required.
        /// POST /api/stacker/assign
        /// </summary>
        [HttpPost("assign")]
        public IActionResult Assign()
        {
            _logger.LogInformation("Assign triggered.");

            // TODO: wire to Aggregate / Service layer when logic is defined
            return Ok(new
            {
                success = true,
                message = "Assign procedure triggered successfully."
            });
        }
    }

    // ── Inline request model (move to Models/Stacker/ when logic is added) ───
    public class ScanRequest
    {
        public string ScannedId { get; set; } = string.Empty;
    }
}