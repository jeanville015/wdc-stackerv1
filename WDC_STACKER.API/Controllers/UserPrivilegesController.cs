using Microsoft.AspNetCore.Mvc;
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Controllers
{
    [ApiController]
    [Route("api/user-privileges")]
    public class UserPrivilegesController : ControllerBase
    {
        private readonly UserPrivilegesService _service;

        public UserPrivilegesController(UserPrivilegesService service)
        {
            _service = service;
        }

        /// <summary>
        /// Returns FEATS privileges for a given employee.
        /// GET /api/user-privileges?employeeName=JSMITH
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
                return BadRequest(new { message = "employeeName query parameter is required." });

            var request = new UserPrivilegesRequest { EmployeeName = employeeName };
            var result = await _service.GetAsync(request);

            if (!result.Success)
                return StatusCode(502, new { result.Message });   // 502 = upstream FEATS failure

            return Ok(result);
        }
    }
}
