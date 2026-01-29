using Erasmus_SSC.Dtos;
using Erasmus_SSC.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Erasmus_SSC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILoginAttemptService _loginAttemptService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ILoginAttemptService loginAttemptService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _loginAttemptService = loginAttemptService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                if (_loginAttemptService.IsLockedOut(request.Email))
                {
                    var remainingSeconds = _loginAttemptService.GetRemainingLockoutSeconds(request.Email);
                    return StatusCode(429, new
                    {
                        message = "Account temporarily locked due to too many failed login attempts.",
                        remainingLockoutSeconds = remainingSeconds
                    });
                }

                var result = await _authService.LoginUserAsync(request);
                if (result == null)
                {
                    var attemptsLeft = _loginAttemptService.RecordFailedAttempt(request.Email);

                    return Unauthorized(new
                    {
                        message = "Invalid username or password.",
                        attempts_left = attemptsLeft
                    });
                }

                _loginAttemptService.RecordSuccessfulLogin(request.Email);

               
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

                Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

                
                return Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken
                });

             
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for identifier {Identifier}", request.Email);
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<TokenResponseDto>> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrWhiteSpace(refreshToken))
                    return BadRequest("Refresh token is missing.");

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);
                if (result == null)
                    return Unauthorized("Invalid or expired refresh token.");

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(7)
                };

              
                if (!string.IsNullOrEmpty(result.RefreshToken))
                {
                    Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);
                }

                return Ok(result); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }


    }
}

