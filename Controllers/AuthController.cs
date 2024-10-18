using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToDoList.Services;
using ToDoList.Models;
using System.Text;
using DotNetEnv;

namespace ToDoList.Controllers
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, IConfiguration config)
        {
            _authService = authService;
            _config = config;
            Env.Load();
        }

        [HttpPost("create")]
        [Authorize(Policy = "CanCreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var newUser = new User { Username = request.Username };
            var result = await _authService.CreateUserAsync(newUser, request.Password, request.Role);

            if (!result)
            {
                return BadRequest(new { Message = "User already exists" });
            }

            return Ok(new { Message = "User created successfully" });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(new { Message = "Invalid username or password" });
            }

            var token = GenerateJwtToken(user);

            return Ok(new { Username = user.Username, Role = user.Role, Token = token });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException()));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds,
                audience: Environment.GetEnvironmentVariable("JWT_ISSUER"),
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER")
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("request-password-reset")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            var user = await _authService.GetUserByUsernameAsync(request.Username);
            if (user == null)
            {
                return BadRequest(new { Message = "User not found" });
            }

            var token = await _authService.GeneratePasswordResetTokenAsync(user);

            return Ok(new { Uid = user.Id, Token = token });
        }

        [HttpGet("verify-reset-token")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyResetToken(string uid, string token)
        {
            var result = await _authService.VerifyPasswordResetTokenAsync(uid, token);
            if (!result)
            {
                return BadRequest(new { Message = "Invalid or expired token" });
            }

            return Ok(new { Message = "Token is valid" });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] SetNewPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request.UserId, request.Token, request.NewPassword);
            if (!result)
            {
                return BadRequest(new { Message = "Invalid token or user" });
            }

            return Ok(new { Message = "Password reset successful" });
        }
    }
}
