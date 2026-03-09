using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ProductsServiceTests
{
    [Fact]
    public async Task DeleteProductAsync_WithExistingLicense_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new ProductsService(
            scope.Context,
            new CreateProductRequestValidator(),
            new UpdateProductRequestValidator(),
            new CreateLicenseRequestValidator(),
            new UpdateLicenseRequestValidator(),
            publisher);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteProductAsync("PRD-001"));
        Assert.Empty(publisher.Events);
    }

    [Fact]
    public async Task CreateLicenseAsync_WithFutureStartDate_PublishesPendingRealtimeEvent()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new ProductsService(
            scope.Context,
            new CreateProductRequestValidator(),
            new UpdateProductRequestValidator(),
            new CreateLicenseRequestValidator(),
            new UpdateLicenseRequestValidator(),
            publisher);

        var response = await service.CreateLicenseAsync(
            "PRD-002",
            new CreateLicenseRequest("CLI-002", "Starter", 450m, "2026-12-01", "2027-12-01"));

        Assert.Equal("Pendente", response.Status);
        Assert.Contains(publisher.Events, x => x.EventType == "license.created");
    }
}
