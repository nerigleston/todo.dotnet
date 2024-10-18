using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ToDoList.Services;
using ToDoList.Models;
using ToDoList.Utils;
using DotNetEnv;

namespace ToDoList.Controllers
{
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly S3Service _s3Service;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, IConfiguration config, S3Service s3Service)
        {
            _authService = authService;
            _config = config;
            _s3Service = s3Service;
            Env.Load();
        }

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromForm] CreateUserRequest request, IFormFile? picture)
        {
            var newUser = new User { Username = request.Username };

            if (picture != null && picture.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{picture.FileName}";
                newUser.PictureUrl = await _s3Service.UploadFileAsync(picture, fileName);
            }

            var result = await _authService.CreateUserAsync(newUser, request.Password, request.Role);

            if (!result)
            {
                return BadRequest(new { Message = "User already exists" });
            }

            return Ok(new { Message = "User created successfully", PictureUrl = newUser.PictureUrl });
        }

        [HttpPatch("{userId}/photo")]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> UpdateUserPhoto(string userId, IFormFile newPhoto)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var photoUrl = await _s3Service.UploadFileAsync(newPhoto, $"{Guid.NewGuid()}_{newPhoto.FileName}");

            Console.WriteLine(photoUrl);

            user.PictureUrl = photoUrl;
            await _authService.UpdateUserAsync(user);

            return Ok(new { message = "Photo updated successfully", PictureUrl = photoUrl });
        }


        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _authService.AuthenticateAsync(request.Username, request.Password);

            if (user == null)
            {
                return Unauthorized(
                new
                {
                    statusCode = 401,
                    message = "Invalid username or password"
                });
            }

            var token = JwtTokenGenerator.GenerateJwtToken(user);

            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
                Token = token
            });
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            return Ok(new { Username = user.Username, Role = user.Role, PictureUrl = user.PictureUrl });
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
