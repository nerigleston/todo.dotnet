using MongoDB.Driver;
using ToDoList.Models;

namespace ToDoList.Data
{
    public class TodoDbContext
    {
        private readonly IMongoDatabase _database;

        public TodoDbContext(IConfiguration config)
        {
            var client = new MongoClient(config.GetConnectionString("MongoDb"));
            _database = client.GetDatabase("ToDoListDb");
        }

        public IMongoCollection<TodoItem> TodoItems => _database.GetCollection<TodoItem>("TodoItems");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
