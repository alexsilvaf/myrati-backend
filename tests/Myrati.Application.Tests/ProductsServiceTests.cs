using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Domain.Clients;
using Myrati.Domain.Products;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ProductsServiceTests
{
    [Fact]
    public async Task DeleteProductAsync_WithExistingLicense_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteProductAsync("PRD-001"));
        Assert.Empty(publisher.Events);
    }

    [Fact]
    public async Task CreateLicenseAsync_WithFutureStartDate_PublishesPendingRealtimeEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        var response = await service.CreateLicenseAsync(
            "PRD-002",
            new CreateLicenseRequest("CLI-002", "Starter", 450m, null, null, "2026-12-01", "2027-12-01"));

        Assert.Equal("Pendente", response.Status);
        Assert.Contains(publisher.Events, x => x.EventType == "license.created");
    }

    [Fact]
    public async Task DeleteProductAsync_WithConnectedUsersAndNoLicenses_RemovesProductAndUsers()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        await scope.Context.AddAsync(new Product
        {
            Id = "PRD-TEST-DELETE",
            Name = "Produto Transitório",
            Description = "Produto temporário para teste de exclusão.",
            Category = "Teste",
            Status = "Em desenvolvimento",
            SalesStrategy = "subscription",
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Version = "1.0.0"
        });

        await scope.Context.AddAsync(new ConnectedUser
        {
            Id = "USR-TEST-DELETE",
            ClientId = "CLI-001",
            ProductId = "PRD-TEST-DELETE",
            Name = "Usuario Temporario",
            Email = "temporario@myrati.com",
            LastActiveDisplay = "Agora",
            Status = "Online"
        });
        await scope.Context.SaveChangesAsync();

        await service.DeleteProductAsync("PRD-TEST-DELETE");

        Assert.DoesNotContain(scope.Context.ProductsSet, product => product.Id == "PRD-TEST-DELETE");
        Assert.DoesNotContain(scope.Context.ConnectedUsersSet, user => user.ProductId == "PRD-TEST-DELETE");
        Assert.Contains(publisher.Events, x => x.EventType == "product.deleted");
    }

    [Fact]
    public async Task CreateTaskAsync_ForDevelopmentProduct_PersistsTaskAndPublishesEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        var response = await service.CreateTaskAsync(
            "PRD-004",
            new CreateProductTaskRequest(
                "SPR-003",
                "Nova automação",
                "Criar fluxo automatizado para agentes.",
                "todo",
                "high",
                "Admin Master",
                ["backend", "automation"]));

        Assert.Equal("PRD-004", response.ProductId);
        Assert.Equal("todo", response.Column);
        Assert.Contains("backend", response.Tags);
        Assert.Contains(publisher.Events, x => x.EventType == "task.created");
    }

    [Fact]
    public async Task DeleteSprintAsync_WithLinkedTasks_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteSprintAsync("PRD-004", "SPR-003"));
    }

    private static ProductsService CreateService(
        Infrastructure.Persistence.MyratiDbContext context,
        TestRealtimeEventPublisher publisher) =>
        new(
            context,
            new CreateProductRequestValidator(),
            new UpdateProductRequestValidator(),
            new CreateLicenseRequestValidator(),
            new UpdateLicenseRequestValidator(),
            new CreateProductSprintRequestValidator(),
            new UpdateProductSprintRequestValidator(),
            new CreateProductTaskRequestValidator(),
            new UpdateProductTaskRequestValidator(),
            publisher);
}
