using Erasmus_SSC.Client.Dtos;
using Erasmus_SSC.Dtos;
using Erasmus_SSC.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Erasmus_SSC.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UserAdminController : ControllerBase
    {
        private readonly IUserAdminService _userAdminService;
        private readonly ILogger<UserAdminController> _logger;

        public UserAdminController(
            IUserAdminService userAdminService,
            ILogger<UserAdminController> logger)
        {
            _userAdminService = userAdminService;
            _logger = logger;
        }
       
        
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<UserDto>>> GetUsers(CancellationToken ct)
        {
            try
            {
                var users = await _userAdminService.GetUsersAsync(ct);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUsers failed");
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Admin creates a new user (default role = User).
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] RegisterRequestDto dto, CancellationToken ct)
        {
            try
            {
                var created = await _userAdminService.CreateUserAsync(dto, ct);
                // 201 + payload
                return Created($"/api/admin/users/{created.Id}", created);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "CreateUser validation error");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                
                _logger.LogWarning(ex, "CreateUser conflict");
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateUser failed");
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }

        /// <summary>
        /// Admin deletes a user by id.
        /// </summary>
        [HttpDelete("{userId:int}")]
        public async Task<IActionResult> DeleteUser([FromRoute] int userId, CancellationToken ct)
        {
            try
            {
                var deleted = await _userAdminService.DeleteUserAsync(userId, ct);
                if (!deleted) return NotFound(new { message = "User not found." });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
               
                _logger.LogWarning(ex, "DeleteUser blocked");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUser failed");
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }

        [HttpPut("{userId:int}")]
        public async Task<ActionResult<UserDto>> UpdateUser(int userId, [FromBody] UpdateUserRequestDto dto, CancellationToken ct)
        {
            try
            {
                var updated = await _userAdminService.UpdateUserAsync(userId, dto, ct);
                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "UpdateUser validation error");
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "UpdateUser failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateUser failed");
                return StatusCode(500, "An internal error occurred. Please try again later.");
            }
        }



    }
}
