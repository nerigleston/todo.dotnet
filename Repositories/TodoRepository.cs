using MongoDB.Driver;
using ToDoList.Models;
using ToDoList.Data;

namespace ToDoList.Repositories
{
    public class TodoRepository
    {
        private readonly TodoDbContext _context;

        public TodoRepository(TodoDbContext context)
        {
            _context = context;
        }

        public async Task<List<TodoItem>> GetAllAsync() => await _context.TodoItems.Find(_ => true).ToListAsync();
        public async Task<TodoItem> GetByIdAsync(string id)
        {
            var filter = Builders<TodoItem>.Filter.Eq(todo => todo.Id, id);
            return await _context.TodoItems.Find(filter).FirstOrDefaultAsync();
        }

        public async Task AddAsync(TodoItem todo) => await _context.TodoItems.InsertOneAsync(todo);
        public async Task UpdateAsync(string id, TodoItem todoIn)
        {
            var filter = Builders<TodoItem>.Filter.Eq(todo => todo.Id, id);
            await _context.TodoItems.ReplaceOneAsync(filter, todoIn);
        }

        public async Task RemoveAsync(string id)
        {
            var filter = Builders<TodoItem>.Filter.Eq(todo => todo.Id, id);
            await _context.TodoItems.DeleteOneAsync(filter);
        }
    }
}
