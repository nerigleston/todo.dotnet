using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using ToDoList.Models;
using System.Text;
using DotNetEnv;

namespace ToDoList.Utils
{
    public static class JwtTokenGenerator
    {
        public static string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("JWT_SECRET is not set")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            Env.Load();

            var claims = new[]
            {
                   new Claim(ClaimTypes.Name, user.Username),
                   new Claim(ClaimTypes.Role, user.Role),
                   new Claim(ClaimTypes.NameIdentifier, user.Id)
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
    }
}
