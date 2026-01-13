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
                if (_loginAttemptService.IsLockedOut(request.UserName))
                {
                    var remainingSeconds = _loginAttemptService.GetRemainingLockoutSeconds(request.UserName);
                    return StatusCode(429, new
                    {
                        message = "Account temporarily locked due to too many failed login attempts.",
                        remainingLockoutSeconds = remainingSeconds
                    });
                }

                var result = await _authService.LoginUserAsync(request);
                if (result == null)
                {
                    var attemptsLeft = _loginAttemptService.RecordFailedAttempt(request.UserName);

                    return Unauthorized(new
                    {
                        message = "Invalid username or password.",
                        attempts_left = attemptsLeft
                    });
                }

                _loginAttemptService.RecordSuccessfulLogin(request.UserName);

               
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
                _logger.LogError(ex, "Login failed for identifier {Identifier}", request.UserName);
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

              
                if (!string.IsNullOrWhiteSpace(result.RefreshToken))
                    Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

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

//using Erasmus_SSC.Dtos;
//using Erasmus_SSC.Interfaces;
//using Erasmus_SSC.Services;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;

//namespace Erasmus_SSC.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class AuthController : ControllerBase
//    {
//        private readonly IAuthService _authService;

//        private readonly ILoginAttemptService _loginAttemptService;
//        private readonly ILogger<AuthController> _logger;

//        public AuthController(
//       IAuthService authService,
//       ILoginAttemptService loginAttemptService,
//       ILogger<AuthController> logger)
//        {
//            _authService = authService;
//            _loginAttemptService = loginAttemptService;
//            _logger = logger;
//        }
//        [HttpPost("login")]
//        public async Task<IActionResult> Login(LoginRequestDto request)
//        {
//            try
//            {
//                if (_loginAttemptService.IsLockedOut(request.UserName))
//                {
//                    var remainingSeconds = _loginAttemptService.GetRemainingLockoutSeconds(request.UserName);
//                    return StatusCode(429, new
//                    {
//                        message = "Account temporarily locked due to too many failed login attempts.",
//                        remainingLockoutSeconds = remainingSeconds
//                    });
//                }

//                var result = await _authService.LoginUserAsync(request);
//                if (result == null)
//                {
//                    var attemptsLeft = _loginAttemptService.RecordFailedAttempt(request.UserName);

//                    return Unauthorized(new
//                    {
//                        message = "Invalid input.",
//                        attempts_left = attemptsLeft + 1
//                    });
//                }

//                _loginAttemptService.RecordSuccessfulLogin(request.UserName);

//                var cookieOptions = new CookieOptions
//                {
//                    HttpOnly = true,
//                    Secure = true,
//                    SameSite = SameSiteMode.None,
//                    Expires = DateTime.UtcNow.AddDays(7)
//                };
//                Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

//                return Ok(new
//                {
//                    accessToken = result.AccessToken,
//                    refreshToken = result.RefreshToken
//                });
//            }

//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Login failed for email {Username}", request.UserName);
//                return StatusCode(500, "An internal error occurred. Please try again later.");
//            }
//        }

//        //public AuthController(IAuthService auth)
//        //{
//        //    _auth = auth;
//        //}


//        //[HttpPost("register")]
//        //[Authorize(Policy = "AdminOnly")]
//        //public async Task<IActionResult> Register(RegisterRequestDto dto)
//        //{
//        //    await _auth.RegisterAsync(dto);
//        //    return Ok();
//        //}

//        //[HttpPost("login")]
//        //[AllowAnonymous]
//        //public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
//        //{
//        //    var result = await _auth.LoginAsync(dto);
//        //    return Ok(result);
//        //}

//        //[HttpPost("refresh")]
//        //[AllowAnonymous]
//        //public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] string refreshToken)
//        //{
//        //    var result = await _auth.RefreshAsync(refreshToken);
//        //    return Ok(result);
//        //}

//        [HttpPost("refresh-token")]
//        public async Task<ActionResult<TokenResponseDto>> RefreshToken()
//        {
//            try
//            {
//                var refreshToken = Request.Cookies["refreshToken"];
//                if (string.IsNullOrEmpty(refreshToken))
//                    return BadRequest("Refresh token is missing or does not match.");

//                // Use IP/device info for token rotation tracking
//                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
//                var device = Request.Headers["User-Agent"].ToString();

//                var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress, device);

//                if (result == null)
//                    return Unauthorized("Invalid or expired refresh token.");

//                // Overwrite cookie with new refresh token
//                var cookieOptions = new CookieOptions
//                {
//                    HttpOnly = true,
//                    Secure = true,
//                    SameSite = SameSiteMode.None,
//                    Expires = DateTime.UtcNow.AddDays(7)
//                };
//                Response.Cookies.Append("refreshToken", result.RefreshToken, cookieOptions);

//                return Ok(new TokenResponseDto
//                {
//                    AccessToken = result.AccessToken
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error refreshing token");
//                return StatusCode(500, "An internal error occurred. Please try again later.");
//            }
//        }

//    }

//}
