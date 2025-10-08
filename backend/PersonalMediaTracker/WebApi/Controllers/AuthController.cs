using System.Security.Claims;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApi.Services;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly SignInManager<ApplicationUser> _signIn;
        private readonly IJwtTokenService _jwt;

        public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IJwtTokenService jwt)
        {
            _users = users;
            _signIn = signIn;
            _jwt = jwt;
        }

        // Simple DTOs local to this controller for clarity
        public record RegisterDto(string Email, string Password);
        public record LoginDto(string Email, string Password);
        public record AuthResponse(string AccessToken, string UserId, string Email);


        // Creates a new user and returns an access token
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Register(RegisterDto dto)
        {
            var user = new ApplicationUser { UserName = dto.Email, Email =  dto.Email };
            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var roles = await _users.GetRolesAsync(user);
            var token = _jwt.CreateAccessToken(user, roles);

            return new AuthResponse(token, user.Id.ToString(), user.Email!);
        }


        // Authenticates a user and returns an access token
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
        {
            var user = await _users.FindByEmailAsync(dto.Email);
            if (user is null) return Unauthorized();

            var valid = await _users.CheckPasswordAsync(user, dto.Password);
            if (!valid) return Unauthorized();

            var roles = await _users.GetRolesAsync(user);
            var token = _jwt.CreateAccessToken(user, roles);

            return new AuthResponse(token, user.Id.ToString(), user.Email!);
        }


        // Returns the current authenticated user's basic profile
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<object>> Me()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id is null) return Unauthorized();

            var user = await _users.FindByIdAsync(id);
            if (user is null) return NotFound();

            return new { user.Id, user.Email, user.UserName  };
        }
    }
}
