using Microsoft.AspNetCore.Mvc;
using WDC_STACKER.API.Aggregate;
using WDC_STACKER.API.Models.Auth;

namespace WDC_STACKER.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthProjectionAggregate _aggregate;

        public AuthController(AuthProjectionAggregate aggregate)
        {
            _aggregate = aggregate;
        }

        /// <summary>
        /// Authenticates the user and returns a session token.
        /// POST /api/auth/login
        /// Body: { "username": "...", "password": "..." }
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { Message = "Incorrect login details" });
            }

            if (request.Username.Contains('\\') || request.Username.Contains('@'))
            {
                return BadRequest(new { Message = "Incorrect login details" });
            }

            var result = await _aggregate.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(new { result.Message });

            return Ok(result);
        }
    }
}
