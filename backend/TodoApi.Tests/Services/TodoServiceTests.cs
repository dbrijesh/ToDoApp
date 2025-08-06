using FluentAssertions;
using TodoApi.Models;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Services;

public class TodoServiceTests
{
    private readonly TodoService _todoService;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public TodoServiceTests()
    {
        _todoService = new TodoService();
    }

    [Fact]
    public async Task GetTodosAsync_WhenNoTodos_ReturnsEmptyList()
    {
        // Act
        var result = await _todoService.GetTodosAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTodoAsync_WithValidRequest_CreatesTodo()
    {
        // Arrange
        var request = new CreateTodoRequest
        {
            Title = "Test Todo",
            Description = "Test Description",
            IsCompleted = false
        };

        // Act
        var result = await _todoService.CreateTodoAsync(request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be(request.Title);
        result.Description.Should().Be(request.Description);
        result.IsCompleted.Should().Be(request.IsCompleted);
        result.UserId.Should().Be(TestUserId);
        result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateTodoAsync_MultipleRequests_GeneratesUniqueIds()
    {
        // Arrange
        var request1 = new CreateTodoRequest { Title = "Todo 1", Description = "Description 1" };
        var request2 = new CreateTodoRequest { Title = "Todo 2", Description = "Description 2" };

        // Act
        var todo1 = await _todoService.CreateTodoAsync(request1, TestUserId);
        var todo2 = await _todoService.CreateTodoAsync(request2, TestUserId);

        // Assert
        todo1.Id.Should().NotBe(todo2.Id);
        todo2.Id.Should().BeGreaterThan(todo1.Id);
    }

    [Fact]
    public async Task GetTodosAsync_WithMultipleTodos_ReturnsUserTodosInDescendingOrder()
    {
        // Arrange
        var request1 = new CreateTodoRequest { Title = "First Todo", Description = "First Description" };
        var request2 = new CreateTodoRequest { Title = "Second Todo", Description = "Second Description" };
        var request3 = new CreateTodoRequest { Title = "Other User Todo", Description = "Other Description" };

        var todo1 = await _todoService.CreateTodoAsync(request1, TestUserId);
        await Task.Delay(10); // Ensure different timestamps
        var todo2 = await _todoService.CreateTodoAsync(request2, TestUserId);
        var otherTodo = await _todoService.CreateTodoAsync(request3, OtherUserId);

        // Act
        var result = await _todoService.GetTodosAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().NotContain(otherTodo);
        result.First().Should().BeEquivalentTo(todo2);
        result.Last().Should().BeEquivalentTo(todo1);
    }

    [Fact]
    public async Task GetTodoAsync_WithExistingTodo_ReturnsTodo()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createdTodo = await _todoService.CreateTodoAsync(request, TestUserId);

        // Act
        var result = await _todoService.GetTodoAsync(createdTodo.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(createdTodo);
    }

    [Fact]
    public async Task GetTodoAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _todoService.GetTodoAsync(999, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTodoAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createdTodo = await _todoService.CreateTodoAsync(request, TestUserId);

        // Act
        var result = await _todoService.GetTodoAsync(createdTodo.Id, OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTodoAsync_WithValidRequest_UpdatesTodo()
    {
        // Arrange
        var createRequest = new CreateTodoRequest { Title = "Original Title", Description = "Original Description" };
        var createdTodo = await _todoService.CreateTodoAsync(createRequest, TestUserId);
        
        var updateRequest = new UpdateTodoRequest 
        { 
            Title = "Updated Title", 
            Description = "Updated Description", 
            IsCompleted = true 
        };

        // Act
        var result = await _todoService.UpdateTodoAsync(createdTodo.Id, updateRequest, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdTodo.Id);
        result.Title.Should().Be(updateRequest.Title);
        result.Description.Should().Be(updateRequest.Description);
        result.IsCompleted.Should().Be(updateRequest.IsCompleted);
        result.UserId.Should().Be(TestUserId);
        result.CreatedDate.Should().Be(createdTodo.CreatedDate);
        result.UpdatedDate.Should().BeAfter(createdTodo.UpdatedDate);
    }

    [Fact]
    public async Task UpdateTodoAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var updateRequest = new UpdateTodoRequest { Title = "Updated Title", Description = "Updated Description" };

        // Act
        var result = await _todoService.UpdateTodoAsync(999, updateRequest, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTodoAsync_WithWrongUserId_ReturnsNull()
    {
        // Arrange
        var createRequest = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createdTodo = await _todoService.CreateTodoAsync(createRequest, TestUserId);
        
        var updateRequest = new UpdateTodoRequest { Title = "Updated Title", Description = "Updated Description" };

        // Act
        var result = await _todoService.UpdateTodoAsync(createdTodo.Id, updateRequest, OtherUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTodoAsync_WithExistingTodo_DeletesTodo()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createdTodo = await _todoService.CreateTodoAsync(request, TestUserId);

        // Act
        var result = await _todoService.DeleteTodoAsync(createdTodo.Id, TestUserId);

        // Assert
        result.Should().BeTrue();
        
        // Verify todo is deleted
        var deletedTodo = await _todoService.GetTodoAsync(createdTodo.Id, TestUserId);
        deletedTodo.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTodoAsync_WithNonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _todoService.DeleteTodoAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTodoAsync_WithWrongUserId_ReturnsFalse()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createdTodo = await _todoService.CreateTodoAsync(request, TestUserId);

        // Act
        var result = await _todoService.DeleteTodoAsync(createdTodo.Id, OtherUserId);

        // Assert
        result.Should().BeFalse();
        
        // Verify todo still exists for original user
        var existingTodo = await _todoService.GetTodoAsync(createdTodo.Id, TestUserId);
        existingTodo.Should().NotBeNull();
    }

    [Fact]
    public async Task Service_ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task<TodoItem>>();
        var userIds = new[] { "user1", "user2", "user3" };

        // Act - Create todos concurrently
        for (int i = 0; i < 100; i++)
        {
            var userId = userIds[i % userIds.Length];
            var request = new CreateTodoRequest { Title = $"Todo {i}", Description = $"Description {i}" };
            tasks.Add(_todoService.CreateTodoAsync(request, userId));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(100);
        results.Select(r => r.Id).Should().OnlyHaveUniqueItems();
        
        // Verify each user has their own todos
        foreach (var userId in userIds)
        {
            var userTodos = await _todoService.GetTodosAsync(userId);
            userTodos.Should().AllSatisfy(t => t.UserId.Should().Be(userId));
        }
    }
}