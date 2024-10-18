using ToDoList.Models;
using ToDoList.Repositories;
using System.Text;

namespace ToDoList.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> CreateUserAsync(User user, string password, string role)
        {
            if (await _userRepository.GetByUsernameAsync(user.Username) != null)
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.Role = role;
            await _userRepository.AddUserAsync(user);
            return true;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }

            return null;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(User user)
        {
            return await Task.Run(() =>
            {
                var tokenString = $"{user.Id}:{DateTime.UtcNow.Ticks}";

                var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(tokenString));

                return token;
            });
        }

        public async Task<bool> VerifyPasswordResetTokenAsync(string uid, string token)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var decodedToken = Encoding.UTF8.GetString(Convert.FromBase64String(token));

                    var tokenParts = decodedToken.Split(':');
                    if (tokenParts.Length != 2) return false;

                    var userIdFromToken = tokenParts[0];
                    var tokenTimestamp = long.Parse(tokenParts[1]);

                    if (userIdFromToken != uid) return false;

                    var tokenDate = new DateTime(tokenTimestamp);
                    if ((DateTime.UtcNow - tokenDate).TotalHours > 1) return false;

                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public async Task<bool> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !await VerifyPasswordResetTokenAsync(userId, token))
            {
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepository.UpdateUserAsync(user);

            return true;
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _userRepository.GetByUsernameAsync(username);
        }
    }
}
