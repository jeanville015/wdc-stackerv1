using Microsoft.AspNetCore.Mvc;
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
        /// Headers: X-Feats-Username, X-Feats-Password
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
                return BadRequest(new { message = "employeeName query parameter is required." });

            var username = Request.Headers["X-Feats-Username"].ToString();
            var password = Request.Headers["X-Feats-Password"].ToString();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return Unauthorized(new { message = "FEATS credentials are required. Provide X-Feats-Username and X-Feats-Password headers." });

            var request = new UserPrivilegesRequest
            {
                EmployeeName = employeeName,
                FeatsUsername = username,
                FeatsPassword = password
            };

            var result = await _service.GetAsync(request);

            if (!result.Success)
                return StatusCode(502, new { result.Message });

            return Ok(result);
        }
    }
}