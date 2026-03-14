using Myrati.Application.Common;
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
            CreatedDate = ApplicationTime.LocalToday()
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

    [Fact]
    public async Task RecordProductionDeploymentAsync_IncrementsProdAndResetsDevVersion()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = CreateService(scope.Context, publisher);

        var sprint = await service.CreateSprintAsync(
            "PRD-004",
            new CreateProductSprintRequest(
                "Sprint 6 - Deploy",
                "2026-03-20",
                "2026-04-03",
                "Planejada"));

        Assert.NotNull(sprint);

        var beforeDeploy = await service.GetProductAsync("PRD-004");
        Assert.Equal("0.6", beforeDeploy.Version);

        var deployed = await service.RecordProductionDeploymentAsync("PRD-004");

        Assert.Equal(1, deployed.ProductionDeploys);
        Assert.Equal(0, deployed.DevSprintsSinceLastDeploy);
        Assert.Equal("1.0", deployed.Version);
        Assert.Contains(publisher.Events, x => x.EventType == "product.deployed");
    }

    private static ProductsService CreateService(
        Infrastructure.Persistence.MyratiDbContext context,
        TestRealtimeEventPublisher publisher) =>
        new(
            context,
            new StubCurrentUserContext("TM-001", "admin@myrati.com", "Super Admin"),
            new CreateProductRequestValidator(),
            new CreateProductSetupRequestValidator(),
            new UpdateProductRequestValidator(),
            new CreateLicenseRequestValidator(),
            new UpdateLicenseRequestValidator(),
            new ImportProductBacklogRequestValidator(),
            new CreateProductSprintRequestValidator(),
            new UpdateProductSprintRequestValidator(),
            new CreateProductTaskRequestValidator(),
            new UpdateProductTaskRequestValidator(),
            new CreateProductExpenseRequestValidator(),
            new UpdateProductExpenseRequestValidator(),
            new AddProductCollaboratorRequestValidator(),
            new UpdateProductCollaboratorRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher());

    private sealed class StubCurrentUserContext(string userId, string email, string role) : Myrati.Application.Abstractions.ICurrentUserContext
    {
        public string? UserId => userId;

        public string? Email => email;

        public string? Role => role;

        public bool IsAuthenticated => true;
    }
}
