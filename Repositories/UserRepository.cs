using MongoDB.Driver;
using ToDoList.Models;
using ToDoList.Data;

namespace ToDoList.Repositories
{
    public class UserRepository
    {
        private readonly TodoDbContext _context;

        public UserRepository(TodoDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.Find(u => u.Username == username).FirstOrDefaultAsync();
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.InsertOneAsync(user);
        }

        public async Task UpdateUserAsync(User user)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);

            var update = Builders<User>.Update
                .Set(u => u.PasswordHash, user.PasswordHash)
                .Set(u => u.PictureUrl, user.PictureUrl);

            await _context.Users.UpdateOneAsync(filter, update);
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

    }
}
