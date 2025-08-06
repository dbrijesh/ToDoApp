using TodoApi.Models;

namespace TodoApi.Services;

public interface ITodoService
{
    Task<IEnumerable<TodoItem>> GetTodosAsync(string userId);
    Task<TodoItem?> GetTodoAsync(int id, string userId);
    Task<TodoItem> CreateTodoAsync(CreateTodoRequest request, string userId);
    Task<TodoItem?> UpdateTodoAsync(int id, UpdateTodoRequest request, string userId);
    Task<bool> DeleteTodoAsync(int id, string userId);
}