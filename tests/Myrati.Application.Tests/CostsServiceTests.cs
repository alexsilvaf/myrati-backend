using Myrati.Application.Common.Exceptions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class CostsServiceTests
{
    [Fact]
    public async Task GetCostsAsync_ReturnsCosts()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = CreateService(scope);

        var result = await service.GetCostsAsync();

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task CreateCostAsync_PersistsAndPublishesEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var backofficePublisher = new TestBackofficeNotificationPublisher();
        var service = new CostsService(
            scope.Context,
            new CreateCompanyCostRequestValidator(),
            new UpdateCompanyCostRequestValidator(),
            publisher,
            backofficePublisher);

        var result = await service.CreateCostAsync(
            new(
                "Novo Custo Teste",
                "Descricao do custo de teste",
                "tools",
                150m,
                "monthly",
                "Vendor Teste",
                "2026-01-01",
                null,
                "Ativo"));

        Assert.Equal("Novo Custo Teste", result.Name);
        Assert.Contains(publisher.Events, x => x.EventType == "company.cost.created");
        Assert.Contains(backofficePublisher.Events, x => x.EventType == "company.cost.created");
    }

    [Fact]
    public async Task UpdateCostAsync_WithInvalidId_ThrowsEntityNotFoundException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = CreateService(scope);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.UpdateCostAsync(
                "CC-INVALID",
                new(
                    "Nome",
                    "Descricao",
                    "tools",
                    100m,
                    "monthly",
                    "Vendor",
                    "2026-01-01",
                    null,
                    "Ativo")));
    }

    [Fact]
    public async Task DeleteCostAsync_RemovesCostAndPublishesEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var backofficePublisher = new TestBackofficeNotificationPublisher();
        var service = new CostsService(
            scope.Context,
            new CreateCompanyCostRequestValidator(),
            new UpdateCompanyCostRequestValidator(),
            publisher,
            backofficePublisher);

        await service.DeleteCostAsync("CC-001");

        Assert.Contains(publisher.Events, x => x.EventType == "company.cost.deleted");
        Assert.Contains(backofficePublisher.Events, x => x.EventType == "company.cost.deleted");
        var costs = await service.GetCostsAsync();
        Assert.DoesNotContain(costs, c => c.Id == "CC-001");
    }

    private static CostsService CreateService(SeededDbContextScope scope) =>
        new(
            scope.Context,
            new CreateCompanyCostRequestValidator(),
            new UpdateCompanyCostRequestValidator(),
            new TestRealtimeEventPublisher(),
            new TestBackofficeNotificationPublisher());
}
