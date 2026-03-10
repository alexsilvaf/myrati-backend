using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductKanbanEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task KanbanCrud_ForDevelopmentProduct_CompletesSuccessfully()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var initialKanbanResponse = await client.GetAsync("/api/v1/backoffice/products/PRD-004/kanban");
        initialKanbanResponse.EnsureSuccessStatusCode();

        var initialKanban = await initialKanbanResponse.Content.ReadFromJsonAsync<ProductKanbanDto>();
        Assert.NotNull(initialKanban);
        Assert.NotEmpty(initialKanban.Sprints);
        Assert.NotEmpty(initialKanban.Tasks);

        var createSprintResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products/PRD-004/sprints",
            new CreateProductSprintRequest(
                "Sprint Automatizada",
                "2026-03-17",
                "2026-03-31",
                "Planejada"));
        createSprintResponse.EnsureSuccessStatusCode();

        var createdSprint = await createSprintResponse.Content.ReadFromJsonAsync<ProductSprintDto>();
        Assert.NotNull(createdSprint);
        Assert.Equal("PRD-004", createdSprint.ProductId);

        var createTaskResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products/PRD-004/tasks",
            new CreateProductTaskRequest(
                createdSprint.Id,
                "Criar tarefa via API",
                "Fluxo de criação automática por agente.",
                "backlog",
                "high",
                "Admin Master",
                ["backend", "automation"]));
        createTaskResponse.EnsureSuccessStatusCode();

        var createdTask = await createTaskResponse.Content.ReadFromJsonAsync<ProductTaskDto>();
        Assert.NotNull(createdTask);
        Assert.Equal(createdSprint.Id, createdTask.SprintId);
        Assert.Contains("automation", createdTask.Tags);

        var updateTaskResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/products/PRD-004/tasks/{createdTask.Id}",
            new UpdateProductTaskRequest(
                createdSprint.Id,
                "Criar tarefa via API",
                "Fluxo movido para execução.",
                "in_progress",
                "critical",
                "Maria Santos",
                ["backend", "automation"]));
        updateTaskResponse.EnsureSuccessStatusCode();

        var updatedTask = await updateTaskResponse.Content.ReadFromJsonAsync<ProductTaskDto>();
        Assert.NotNull(updatedTask);
        Assert.Equal("in_progress", updatedTask.Column);
        Assert.Equal("critical", updatedTask.Priority);

        var deleteTaskResponse = await client.DeleteAsync($"/api/v1/backoffice/products/PRD-004/tasks/{createdTask.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteTaskResponse.StatusCode);

        var deleteSprintResponse = await client.DeleteAsync($"/api/v1/backoffice/products/PRD-004/sprints/{createdSprint.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteSprintResponse.StatusCode);
    }

    [Fact]
    public async Task KanbanCrud_ForNonDevelopmentProduct_ReturnsConflict()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products/PRD-001/sprints",
            new CreateProductSprintRequest(
                "Sprint inválida",
                "2026-03-17",
                "2026-03-31",
                "Planejada"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(payload);
        Assert.Contains("kanban", payload.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSprint_WithLinkedTasks_ReturnsConflict()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.DeleteAsync("/api/v1/backoffice/products/PRD-004/sprints/SPR-003");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(payload);
        Assert.Contains("tarefas", payload.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
