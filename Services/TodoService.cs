using ToDoList.Models;
using ToDoList.Repositories;

namespace ToDoList.Services
{
    public class TodoService
    {
        private readonly TodoRepository _repository;

        public TodoService(TodoRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TodoItem>> GetTodosByUserIdAsync(string userId)
        {
            return await _repository.GetByUserIdAsync(userId);
        }

        public async Task<TodoItem> GetByIdAsync(string id, string userId)
        {
            return await _repository.GetByIdAndUserIdAsync(id, userId);
        }

        public async Task CreateAsync(TodoItem todo) => await _repository.AddAsync(todo);
        public async Task UpdateAsync(string id, TodoItem todo) => await _repository.UpdateAsync(id, todo);
        public async Task DeleteAsync(string id) => await _repository.RemoveAsync(id);
    }
}
