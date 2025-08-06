using TodoApi.Models;
using System.Collections.Concurrent;

namespace TodoApi.Services;

public class TodoService : ITodoService
{
    private readonly ConcurrentDictionary<int, TodoItem> _todos = new();
    private int _nextId = 1;

    public Task<IEnumerable<TodoItem>> GetTodosAsync(string userId)
    {
        var userTodos = _todos.Values.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedDate);
        return Task.FromResult<IEnumerable<TodoItem>>(userTodos);
    }

    public Task<TodoItem?> GetTodoAsync(int id, string userId)
    {
        _todos.TryGetValue(id, out var todo);
        if (todo?.UserId != userId)
        {
            return Task.FromResult<TodoItem?>(null);
        }
        return Task.FromResult<TodoItem?>(todo);
    }

    public Task<TodoItem> CreateTodoAsync(CreateTodoRequest request, string userId)
    {
        var todo = new TodoItem
        {
            Id = Interlocked.Increment(ref _nextId),
            Title = request.Title,
            Description = request.Description,
            IsCompleted = request.IsCompleted,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            UserId = userId
        };

        _todos[todo.Id] = todo;
        return Task.FromResult(todo);
    }

    public Task<TodoItem?> UpdateTodoAsync(int id, UpdateTodoRequest request, string userId)
    {
        if (!_todos.TryGetValue(id, out var todo) || todo.UserId != userId)
        {
            return Task.FromResult<TodoItem?>(null);
        }

        todo.Title = request.Title;
        todo.Description = request.Description;
        todo.IsCompleted = request.IsCompleted;
        todo.UpdatedDate = DateTime.UtcNow;

        return Task.FromResult<TodoItem?>(todo);
    }

    public Task<bool> DeleteTodoAsync(int id, string userId)
    {
        if (!_todos.TryGetValue(id, out var todo) || todo.UserId != userId)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(_todos.TryRemove(id, out _));
    }
}