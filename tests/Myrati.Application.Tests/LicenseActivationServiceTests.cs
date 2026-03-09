using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Domain.Products;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class LicenseActivationServiceTests
{
    [Fact]
    public async Task ActivateAsync_WithMatchingProductAndLicense_ReturnsActivationPayload()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await scope.Context.AddAsync(new License
        {
            Id = "TEST-ACTV-AAAA-BBBB",
            ClientId = "CLI-001",
            ProductId = "PRD-001",
            Plan = "Starter",
            MaxUsers = 10,
            ActiveUsers = 3,
            Status = "Ativa",
            StartDate = today.AddDays(-1),
            ExpiryDate = today.AddDays(30),
            MonthlyValue = 990m
        });
        await scope.Context.SaveChangesAsync();

        var service = new LicenseActivationService(scope.Context, new LicenseActivationRequestValidator());

        var response = await service.ActivateAsync(new LicenseActivationRequest("prd-001", "test-actv-aaaa-bbbb"));

        Assert.Equal("TEST-ACTV-AAAA-BBBB", response.LicenseId);
        Assert.Equal("PRD-001", response.ProductId);
        Assert.Equal("CLI-001", response.ClientId);
        Assert.Equal("Ativa", response.Status);
        Assert.Equal("Ativação autorizada para o produto informado.", response.Message);
    }

    [Fact]
    public async Task ActivateAsync_WithLicenseFromAnotherProduct_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await scope.Context.AddAsync(new License
        {
            Id = "TEST-ACTV-CCCC-DDDD",
            ClientId = "CLI-001",
            ProductId = "PRD-001",
            Plan = "Starter",
            MaxUsers = 10,
            ActiveUsers = 1,
            Status = "Ativa",
            StartDate = today.AddDays(-1),
            ExpiryDate = today.AddDays(30),
            MonthlyValue = 990m
        });
        await scope.Context.SaveChangesAsync();

        var service = new LicenseActivationService(scope.Context, new LicenseActivationRequestValidator());

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.ActivateAsync(new LicenseActivationRequest("PRD-002", "TEST-ACTV-CCCC-DDDD")));

        Assert.Equal("A licença informada não pertence ao produto solicitado.", exception.Message);
    }
}
