using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using ToDoList.Services;
using ToDoList.Models;
using Newtonsoft.Json;
using ToDoList.Utils;
using System.Text;
using DotNetEnv;

namespace ToDoList.Controllers
{
    [Route("api/todo")]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly TodoService _todoService;
        private readonly IConfiguration _config;

        public TodoController(TodoService todoService, IConfiguration config)
        {
            _todoService = todoService;
            _config = config;
            Env.Load();
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanView")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var todos = await _todoService.GetTodosByUserIdAsync(userId);
                return Ok(todos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanView")]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { Message = "Invalid ID" });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            try
            {
                var todo = await _todoService.GetByIdAsync(id, userId);

                if (todo == null)
                {
                    return NotFound(new { Message = "Todo not found" });
                }

                return Ok(todo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanCreate")]
        public async Task<IActionResult> Create([FromBody] TodoItem newTodo)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                newTodo.IsCompleted = false;
                newTodo.UserId = userId;

                await _todoService.CreateAsync(newTodo);
                return CreatedAtAction(nameof(GetById), new { id = newTodo.Id }, newTodo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanDelete")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var existingTodo = await _todoService.GetByIdAsync(id, userId);

                if (existingTodo == null || existingTodo.UserId != userId)
                {
                    return NotFound(new { Message = "Todo not found or you're not authorized to delete this todo" });
                }

                await _todoService.DeleteAsync(id);
                return Ok(new { Message = "Todo deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }

        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanEdit")]
        public async Task<IActionResult> ToggleIsCompleted(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest(new { Message = "Invalid ID" });
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User not authenticated" });
                }

                var todo = await _todoService.GetByIdAsync(id, userId);

                if (todo == null)
                {
                    return NotFound(new { Message = "Todo not found" });
                }

                if (todo.UserId != userId)
                {
                    return Unauthorized(new { Message = "You're not authorized to update this todo" });
                }

                todo.IsCompleted = !todo.IsCompleted;

                await _todoService.UpdateAsync(id, todo);

                return Ok(new { Message = "Todo status updated successfully", IsCompleted = todo.IsCompleted });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }

        [HttpGet("ask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "CanView")]
        public async Task<IActionResult> GetAllAndAsk([FromQuery] string question)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "User is not authorized." });
                }

                var todos = await _todoService.GetTodosByUserIdAsync(userId);

                if (todos == null || todos.Count == 0)
                {
                    return NotFound(new { Message = "No todos found." });
                }

                var todosText = string.Join(", ", todos.Select(t =>
                        $"(Todo: {t.Title} Concluído: {t.IsCompleted}, Criado em: {(t.CreatedAt.ToUniversalTime().AddHours(-3)).ToString("dd-MM-yyyy HH:mm:ss")})"
                    )
                );

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = $"Responda sempre dentro do contexto do ToDo",
                                },
                                new
                                {
                                    text = $"isCompleted = true se a tarefa foi concluída, isCompleted = false se a tarefa não foi concluída"
                                },
                                new
                                {
                                    text = $"ToDos: {todosText}"
                                },
                                new
                                {
                                    text = $"Pergunta: {question}"
                                },
                                new
                                {
                                    text = $"Data e Hora Atual: {DateTime.Now.ToUniversalTime().AddHours(-3).ToString("dd-MM-yyyy HH:mm:ss")}"
                                },
                            }
                        }
                    }
                };

                var jsonData = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var apiKey = Environment.GetEnvironmentVariable("GOOGLE_GEMINI_API_KEY");
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent?key={apiKey}";
                var response = await new HttpClient().PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { Message = "Error from external API", Detail = await response.Content.ReadAsStringAsync() });
                }

                var apiResponseContent = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(apiResponseContent);

                var answerText = jsonResponse["candidates"]?.FirstOrDefault()?["content"]?["parts"]?.FirstOrDefault()?["text"]?.ToString();

                if (string.IsNullOrEmpty(answerText))
                {
                    return StatusCode(500, new { Message = "Failed to retrieve a valid answer from the API." });
                }

                var cleanAnswerText = Helpers.RemoveHtmlTags(answerText);

                return Ok(new { Answer = cleanAnswerText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal Server Error", Detail = ex.Message });
            }
        }
    }
}
