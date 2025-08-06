using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using TodoApi.Controllers;
using TodoApi.Models;
using TodoApi.Services;
using Xunit;

namespace TodoApi.Tests.Controllers;

public class TodosControllerTests
{
    private readonly Mock<ITodoService> _mockTodoService;
    private readonly Mock<ILogger<TodosController>> _mockLogger;
    private readonly TodosController _controller;
    private const string TestUserId = "test-user-123";

    public TodosControllerTests()
    {
        _mockTodoService = new Mock<ITodoService>();
        _mockLogger = new Mock<ILogger<TodosController>>();
        _controller = new TodosController(_mockTodoService.Object, _mockLogger.Object);
        
        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }

    [Fact]
    public async Task GetTodos_ReturnsOkWithTodos()
    {
        // Arrange
        var expectedTodos = new List<TodoItem>
        {
            new TodoItem { Id = 1, Title = "Test Todo 1", Description = "Description 1", UserId = TestUserId },
            new TodoItem { Id = 2, Title = "Test Todo 2", Description = "Description 2", UserId = TestUserId }
        };
        _mockTodoService.Setup(s => s.GetTodosAsync(TestUserId)).ReturnsAsync(expectedTodos);

        // Act
        var result = await _controller.GetTodos();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var todos = okResult.Value.Should().BeAssignableTo<IEnumerable<TodoItem>>().Subject;
        todos.Should().BeEquivalentTo(expectedTodos);
        _mockTodoService.Verify(s => s.GetTodosAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetTodos_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        _mockTodoService.Setup(s => s.GetTodosAsync(TestUserId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTodos();

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Internal server error");
    }

    [Fact]
    public async Task GetTodo_WithExistingTodo_ReturnsOkWithTodo()
    {
        // Arrange
        var todoId = 1;
        var expectedTodo = new TodoItem { Id = todoId, Title = "Test Todo", Description = "Description", UserId = TestUserId };
        _mockTodoService.Setup(s => s.GetTodoAsync(todoId, TestUserId)).ReturnsAsync(expectedTodo);

        // Act
        var result = await _controller.GetTodo(todoId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var todo = okResult.Value.Should().BeOfType<TodoItem>().Subject;
        todo.Should().BeEquivalentTo(expectedTodo);
    }

    [Fact]
    public async Task GetTodo_WithNonExistentTodo_ReturnsNotFound()
    {
        // Arrange
        var todoId = 999;
        _mockTodoService.Setup(s => s.GetTodoAsync(todoId, TestUserId)).ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _controller.GetTodo(todoId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetTodo_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var todoId = 1;
        _mockTodoService.Setup(s => s.GetTodoAsync(todoId, TestUserId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetTodo(todoId);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateTodo_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "New Todo", Description = "New Description" };
        var createdTodo = new TodoItem { Id = 1, Title = request.Title, Description = request.Description, UserId = TestUserId };
        _mockTodoService.Setup(s => s.CreateTodoAsync(request, TestUserId)).ReturnsAsync(createdTodo);

        // Act
        var result = await _controller.CreateTodo(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(TodosController.GetTodo));
        createdResult.RouteValues!["id"].Should().Be(createdTodo.Id);
        var todo = createdResult.Value.Should().BeOfType<TodoItem>().Subject;
        todo.Should().BeEquivalentTo(createdTodo);
    }

    [Fact]
    public async Task CreateTodo_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "", Description = "Description" };

        // Act
        var result = await _controller.CreateTodo(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Title is required");
        _mockTodoService.Verify(s => s.CreateTodoAsync(It.IsAny<CreateTodoRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateTodo_WithWhitespaceTitle_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "   ", Description = "Description" };

        // Act
        var result = await _controller.CreateTodo(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Title is required");
    }

    [Fact]
    public async Task CreateTodo_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateTodoRequest { Title = "New Todo", Description = "Description" };
        _mockTodoService.Setup(s => s.CreateTodoAsync(request, TestUserId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateTodo(request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateTodo_WithValidRequest_ReturnsOkWithUpdatedTodo()
    {
        // Arrange
        var todoId = 1;
        var request = new UpdateTodoRequest { Title = "Updated Todo", Description = "Updated Description", IsCompleted = true };
        var updatedTodo = new TodoItem { Id = todoId, Title = request.Title, Description = request.Description, IsCompleted = request.IsCompleted, UserId = TestUserId };
        _mockTodoService.Setup(s => s.UpdateTodoAsync(todoId, request, TestUserId)).ReturnsAsync(updatedTodo);

        // Act
        var result = await _controller.UpdateTodo(todoId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var todo = okResult.Value.Should().BeOfType<TodoItem>().Subject;
        todo.Should().BeEquivalentTo(updatedTodo);
    }

    [Fact]
    public async Task UpdateTodo_WithNonExistentTodo_ReturnsNotFound()
    {
        // Arrange
        var todoId = 999;
        var request = new UpdateTodoRequest { Title = "Updated Todo", Description = "Updated Description" };
        _mockTodoService.Setup(s => s.UpdateTodoAsync(todoId, request, TestUserId)).ReturnsAsync((TodoItem?)null);

        // Act
        var result = await _controller.UpdateTodo(todoId, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateTodo_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var todoId = 1;
        var request = new UpdateTodoRequest { Title = "", Description = "Description" };

        // Act
        var result = await _controller.UpdateTodo(todoId, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Title is required");
        _mockTodoService.Verify(s => s.UpdateTodoAsync(It.IsAny<int>(), It.IsAny<UpdateTodoRequest>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTodo_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var todoId = 1;
        var request = new UpdateTodoRequest { Title = "Updated Todo", Description = "Description" };
        _mockTodoService.Setup(s => s.UpdateTodoAsync(todoId, request, TestUserId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateTodo(todoId, request);

        // Assert
        var statusResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task DeleteTodo_WithExistingTodo_ReturnsNoContent()
    {
        // Arrange
        var todoId = 1;
        _mockTodoService.Setup(s => s.DeleteTodoAsync(todoId, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteTodo(todoId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteTodo_WithNonExistentTodo_ReturnsNotFound()
    {
        // Arrange
        var todoId = 999;
        _mockTodoService.Setup(s => s.DeleteTodoAsync(todoId, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteTodo(todoId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteTodo_WhenServiceThrows_ReturnsInternalServerError()
    {
        // Arrange
        var todoId = 1;
        _mockTodoService.Setup(s => s.DeleteTodoAsync(todoId, TestUserId)).ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteTodo(todoId);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public void GetUserId_WithNameIdentifierClaim_ReturnsCorrectUserId()
    {
        // The user ID should be extracted correctly from claims
        // This is tested implicitly in other tests, but we can verify the behavior
        
        // Act - call any method that uses GetUserId internally
        var result = _controller.GetTodos();

        // Assert - verify the service was called with the correct user ID
        _mockTodoService.Verify(s => s.GetTodosAsync(TestUserId), Times.Once);
    }
}