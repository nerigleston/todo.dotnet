using ToDoList.Models;
using MongoDB.Driver;
using DotNetEnv;

namespace ToDoList.Data
{
    public class TodoDbContext
    {
        private readonly IMongoDatabase _database;

        public TodoDbContext(IConfiguration config)
        {
            Env.Load();
            var client = new MongoClient(Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING"));
            _database = client.GetDatabase("ToDoListDb");
        }

        public IMongoCollection<TodoItem> TodoItems => _database.GetCollection<TodoItem>("TodoItems");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
    }
}
