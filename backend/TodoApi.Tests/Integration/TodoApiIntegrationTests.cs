using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TodoApi.Models;
using Xunit;

namespace TodoApi.Tests.Integration;

public class TodoApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private const string TestUserId = "integration-test-user";

    public TodoApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace JWT authentication with test authentication
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options => { });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetTodos_WhenAuthenticated_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/todos");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTodo_WithValidData_CreatesAndReturnsTodo()
    {
        // Arrange
        var createRequest = new CreateTodoRequest
        {
            Title = "Integration Test Todo",
            Description = "This is a test todo for integration testing",
            IsCompleted = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodo = await response.Content.ReadFromJsonAsync<TodoItem>();
        createdTodo.Should().NotBeNull();
        createdTodo!.Id.Should().BeGreaterThan(0);
        createdTodo.Title.Should().Be(createRequest.Title);
        createdTodo.Description.Should().Be(createRequest.Description);
        createdTodo.IsCompleted.Should().Be(createRequest.IsCompleted);
        createdTodo.UserId.Should().Be(TestUserId);
        createdTodo.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify Location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/todos/{createdTodo.Id}");
    }

    [Fact]
    public async Task CreateTodo_WithEmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = new CreateTodoRequest
        {
            Title = "",
            Description = "Description",
            IsCompleted = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/todos", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("Title is required");
    }

    [Fact]
    public async Task GetTodo_WithExistingId_ReturnsTodo()
    {
        // Arrange - Create a todo first
        var createRequest = new CreateTodoRequest { Title = "Test Todo", Description = "Test Description" };
        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        // Act
        var response = await _client.GetAsync($"/api/todos/{createdTodo!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var todo = await response.Content.ReadFromJsonAsync<TodoItem>();
        todo.Should().BeEquivalentTo(createdTodo);
    }

    [Fact]
    public async Task GetTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/todos/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTodo_WithValidData_UpdatesAndReturnsTodo()
    {
        // Arrange - Create a todo first
        var createRequest = new CreateTodoRequest { Title = "Original Title", Description = "Original Description" };
        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated Title",
            Description = "Updated Description",
            IsCompleted = true
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/todos/{createdTodo!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTodo = await response.Content.ReadFromJsonAsync<TodoItem>();
        updatedTodo.Should().NotBeNull();
        updatedTodo!.Id.Should().Be(createdTodo.Id);
        updatedTodo.Title.Should().Be(updateRequest.Title);
        updatedTodo.Description.Should().Be(updateRequest.Description);
        updatedTodo.IsCompleted.Should().Be(updateRequest.IsCompleted);
        updatedTodo.CreatedDate.Should().Be(createdTodo.CreatedDate);
        updatedTodo.UpdatedDate.Should().BeAfter(createdTodo.UpdatedDate);
    }

    [Fact]
    public async Task UpdateTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = new UpdateTodoRequest { Title = "Updated Title", Description = "Updated Description" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/todos/999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_WithExistingId_DeletesTodo()
    {
        // Arrange - Create a todo first
        var createRequest = new CreateTodoRequest { Title = "Todo to Delete", Description = "This will be deleted" };
        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo!.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify todo is deleted
        var getResponse = await _client.GetAsync($"/api/todos/{createdTodo.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTodo_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/todos/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TodoWorkflow_CreateUpdateDeleteFlow_WorksCorrectly()
    {
        // 1. Create todo
        var createRequest = new CreateTodoRequest
        {
            Title = "Workflow Test Todo",
            Description = "Testing the complete workflow",
            IsCompleted = false
        };

        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoItem>();

        // 2. Get all todos - should contain our todo
        var getAllResponse = await _client.GetAsync("/api/todos");
        var allTodos = await getAllResponse.Content.ReadFromJsonAsync<List<TodoItem>>();
        allTodos.Should().Contain(t => t.Id == createdTodo!.Id);

        // 3. Update todo
        var updateRequest = new UpdateTodoRequest
        {
            Title = "Updated Workflow Todo",
            Description = "Updated description",
            IsCompleted = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/todos/{createdTodo!.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTodo = await updateResponse.Content.ReadFromJsonAsync<TodoItem>();
        updatedTodo!.IsCompleted.Should().BeTrue();

        // 4. Delete todo
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{createdTodo.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify deletion
        var getFinalResponse = await _client.GetAsync("/api/todos");
        var finalTodos = await getFinalResponse.Content.ReadFromJsonAsync<List<TodoItem>>();
        finalTodos.Should().NotContain(t => t.Id == createdTodo.Id);
    }

    [Fact]
    public async Task CreateMultipleTodos_ReturnsInCorrectOrder()
    {
        // Arrange & Act - Create multiple todos with delays
        var todo1 = await CreateTodoAsync("First Todo", "First Description");
        await Task.Delay(10); // Ensure different timestamps
        var todo2 = await CreateTodoAsync("Second Todo", "Second Description");
        await Task.Delay(10);
        var todo3 = await CreateTodoAsync("Third Todo", "Third Description");

        // Get all todos
        var response = await _client.GetAsync("/api/todos");
        var todos = await response.Content.ReadFromJsonAsync<List<TodoItem>>();

        // Assert - Should be in descending order by creation date (newest first)
        todos.Should().HaveCount(3);
        todos[0].Title.Should().Be("Third Todo");
        todos[1].Title.Should().Be("Second Todo");
        todos[2].Title.Should().Be("First Todo");
    }

    private async Task<TodoItem> CreateTodoAsync(string title, string description)
    {
        var request = new CreateTodoRequest { Title = title, Description = description };
        var response = await _client.PostAsJsonAsync("/api/todos", request);
        return (await response.Content.ReadFromJsonAsync<TodoItem>())!;
    }
}

public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "integration-test-user"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}