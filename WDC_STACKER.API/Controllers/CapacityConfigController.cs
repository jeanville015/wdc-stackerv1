using Microsoft.AspNetCore.Mvc;
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Controllers
{
    [ApiController]
    [Route("api/capacity-config")]
    public class CapacityConfigController : ControllerBase
    {
        private readonly CapacityConfigService _service;

        public CapacityConfigController(CapacityConfigService service)
        {
            _service = service;
        }
        private string GetClientKey()
        {
            var clientKey = Request.Headers["X-Stacker-Client"].ToString();
            return string.IsNullOrWhiteSpace(clientKey) ? "WDC_STACKER.CLIENT.PWD" : clientKey;
        }

        // READ - GET /api/capacity-config
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var config = await _service.GetAsync(GetClientKey());
            return Ok(config);
        }

        // UPDATE (full) - PUT /api/capacity-config
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CapacityConfig config)
        {
            await _service.SaveAsync(config, GetClientKey());
            return Ok(config);
        }

        // UPDATE (partial) - PATCH /api/capacity-config
        [HttpPatch]
        public async Task<IActionResult> Patch([FromBody] CapacityConfig config)
        {
            var existing = await _service.GetAsync(GetClientKey());

            // Only update fields that are non-zero (sent by client)
            if (config.RACK_COUNT > 0) existing.RACK_COUNT = config.RACK_COUNT;
            if (config.LAYER_COUNT > 0) existing.LAYER_COUNT = config.LAYER_COUNT;
            if (config.BOX_COUNT > 0) existing.BOX_COUNT = config.BOX_COUNT;
            if (config.TARGET_QTY > 0) existing.TARGET_QTY = config.TARGET_QTY;
            if (config.TARGET_TRAY_COUNT > 0) existing.TARGET_TRAY_COUNT = config.TARGET_TRAY_COUNT;

            await _service.SaveAsync(config, GetClientKey());
            return Ok(existing);
        }

        // RESET to defaults - DELETE /api/capacity-config/reset
        [HttpDelete("reset")]
        public async Task<IActionResult> Reset()
        {
            await _service.ResetAsync(GetClientKey());
            var config = await _service.GetAsync(GetClientKey());
            return Ok(config);
        }
    }
}
