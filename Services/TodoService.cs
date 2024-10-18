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

        public async Task<List<TodoItem>> GetAllAsync() => await _repository.GetAllAsync();
        public async Task<TodoItem> GetByIdAsync(string id) => await _repository.GetByIdAsync(id);
        public async Task CreateAsync(TodoItem todo) => await _repository.AddAsync(todo);
        public async Task UpdateAsync(string id, TodoItem todo) => await _repository.UpdateAsync(id, todo);
        public async Task DeleteAsync(string id) => await _repository.RemoveAsync(id);
    }
}
