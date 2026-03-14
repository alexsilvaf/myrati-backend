using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class CostsAndExpensesEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProductExpenseEndpoints_SupportCrudFlow()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Custos {suffix}",
                "Produto para validar gastos.",
                "Financeiro",
                "Em desenvolvimento",
                "subscription",
                [
                    new UpsertProductPlanRequest("Starter", null, 199m, null, null, null, null)
                ]));
        productResponse.EnsureSuccessStatusCode();

        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(product);

        var createExpenseResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{product.Id}/expenses",
            new CreateProductExpenseRequest(
                "AWS RDS",
                "hosting",
                480m,
                "monthly",
                "Banco principal"));
        createExpenseResponse.EnsureSuccessStatusCode();

        var createdExpense = await createExpenseResponse.Content.ReadFromJsonAsync<ProductExpenseDto>();
        Assert.NotNull(createdExpense);
        Assert.Equal(product.Id, createdExpense.ProductId);
        Assert.Equal("AWS RDS", createdExpense.Name);

        var listResponse = await client.GetAsync($"/api/v1/backoffice/products/{product.Id}/expenses");
        listResponse.EnsureSuccessStatusCode();

        var listedExpenses = await listResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductExpenseDto>>();
        Assert.NotNull(listedExpenses);
        Assert.Contains(listedExpenses, expense => expense.Id == createdExpense.Id);

        var updateExpenseResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/products/{product.Id}/expenses/{createdExpense.Id}",
            new UpdateProductExpenseRequest(
                "AWS RDS Multi-AZ",
                "hosting",
                650m,
                "monthly",
                "Upgrade de disponibilidade"));
        updateExpenseResponse.EnsureSuccessStatusCode();

        var updatedExpense = await updateExpenseResponse.Content.ReadFromJsonAsync<ProductExpenseDto>();
        Assert.NotNull(updatedExpense);
        Assert.Equal(createdExpense.Id, updatedExpense.Id);
        Assert.Equal("AWS RDS Multi-AZ", updatedExpense.Name);
        Assert.Equal(650m, updatedExpense.Amount);

        var deleteExpenseResponse = await client.DeleteAsync(
            $"/api/v1/backoffice/products/{product.Id}/expenses/{createdExpense.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteExpenseResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/api/v1/backoffice/products/{product.Id}/expenses");
        afterDeleteResponse.EnsureSuccessStatusCode();

        var expensesAfterDelete = await afterDeleteResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductExpenseDto>>();
        Assert.NotNull(expensesAfterDelete);
        Assert.DoesNotContain(expensesAfterDelete, expense => expense.Id == createdExpense.Id);
    }

    [Theory]
    [InlineData("development")]
    [InlineData("revenue_share")]
    public async Task ProductMonthlyRevenue_UsesMaintenanceRevenueOnlyWhenProductIsActive(string salesStrategy)
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Receita {suffix}",
                "Produto para validar receita mensal derivada da manutenção.",
                "Financeiro",
                "Ativo",
                salesStrategy,
                [
                    BuildMaintenancePlanRequest(salesStrategy)
                ]));
        createProductResponse.EnsureSuccessStatusCode();

        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(product);

        var createMonthlyExpenseResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{product.Id}/expenses",
            new CreateProductExpenseRequest(
                "Hospedagem",
                "hosting",
                70m,
                "monthly",
                "Infra recorrente"));
        createMonthlyExpenseResponse.EnsureSuccessStatusCode();

        var createAnnualExpenseResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{product.Id}/expenses",
            new CreateProductExpenseRequest(
                "Dominio",
                "domain",
                80m,
                "annual",
                "Renovacao anual"));
        createAnnualExpenseResponse.EnsureSuccessStatusCode();

        var activeDetailResponse = await client.GetAsync($"/api/v1/backoffice/products/{product.Id}");
        activeDetailResponse.EnsureSuccessStatusCode();

        var activeProduct = await activeDetailResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(activeProduct);
        Assert.Equal(100m, activeProduct.MonthlyRevenue);

        var activeListResponse = await client.GetAsync("/api/v1/backoffice/products");
        activeListResponse.EnsureSuccessStatusCode();

        var activeProducts = await activeListResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();
        Assert.NotNull(activeProducts);
        Assert.Equal(100m, Assert.Single(activeProducts, item => item.Id == product.Id).MonthlyRevenue);

        var updateProductResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/products/{product.Id}",
            new UpdateProductRequest(
                activeProduct.Name,
                activeProduct.Description,
                activeProduct.Category,
                "Em desenvolvimento",
                salesStrategy,
                [
                    BuildMaintenancePlanRequest(salesStrategy)
                ]));
        updateProductResponse.EnsureSuccessStatusCode();

        var draftProduct = await updateProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(draftProduct);
        Assert.Equal(0m, draftProduct.MonthlyRevenue);

        var draftListResponse = await client.GetAsync("/api/v1/backoffice/products");
        draftListResponse.EnsureSuccessStatusCode();

        var draftProducts = await draftListResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();
        Assert.NotNull(draftProducts);
        Assert.Equal(0m, Assert.Single(draftProducts, item => item.Id == product.Id).MonthlyRevenue);
    }

    [Fact]
    public async Task CompanyCostEndpoints_SupportCrudFlow()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var initialResponse = await client.GetAsync("/api/v1/backoffice/costs");
        initialResponse.EnsureSuccessStatusCode();

        var initialCosts = await initialResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<CompanyCostDto>>();
        Assert.NotNull(initialCosts);

        var createCostResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/costs",
            new CreateCompanyCostRequest(
                $"Datadog {suffix}",
                "Observabilidade da plataforma.",
                "tools",
                520m,
                "monthly",
                "Datadog",
                "2026-03-10",
                "2026-04-10",
                "Ativo"));
        Assert.Equal(HttpStatusCode.Created, createCostResponse.StatusCode);

        var createdCost = await createCostResponse.Content.ReadFromJsonAsync<CompanyCostDto>();
        Assert.NotNull(createdCost);
        Assert.Equal($"Datadog {suffix}", createdCost.Name);

        var listAfterCreateResponse = await client.GetAsync("/api/v1/backoffice/costs");
        listAfterCreateResponse.EnsureSuccessStatusCode();

        var costsAfterCreate = await listAfterCreateResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<CompanyCostDto>>();
        Assert.NotNull(costsAfterCreate);
        Assert.Contains(costsAfterCreate, cost => cost.Id == createdCost.Id);

        var updateCostResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/costs/{createdCost.Id}",
            new UpdateCompanyCostRequest(
                $"Datadog Enterprise {suffix}",
                "Observabilidade e APM.",
                "tools",
                640m,
                "monthly",
                "Datadog",
                "2026-03-10",
                "2026-04-10",
                "Ativo"));
        updateCostResponse.EnsureSuccessStatusCode();

        var updatedCost = await updateCostResponse.Content.ReadFromJsonAsync<CompanyCostDto>();
        Assert.NotNull(updatedCost);
        Assert.Equal(createdCost.Id, updatedCost.Id);
        Assert.Equal($"Datadog Enterprise {suffix}", updatedCost.Name);
        Assert.Equal(640m, updatedCost.Amount);

        var deleteCostResponse = await client.DeleteAsync($"/api/v1/backoffice/costs/{createdCost.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCostResponse.StatusCode);

        var listAfterDeleteResponse = await client.GetAsync("/api/v1/backoffice/costs");
        listAfterDeleteResponse.EnsureSuccessStatusCode();

        var costsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<CompanyCostDto>>();
        Assert.NotNull(costsAfterDelete);
        Assert.DoesNotContain(costsAfterDelete, cost => cost.Id == createdCost.Id);
    }

    [Fact]
    public async Task CompanyCosts_GetForDeveloper_ReturnsForbidden()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsync("bruno@myrati.com", "Myrati@123");
        client.UseBearerToken(auth.AccessToken);

        var response = await client.GetAsync("/api/v1/backoffice/costs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static UpsertProductPlanRequest BuildMaintenancePlanRequest(string salesStrategy) =>
        salesStrategy switch
        {
            "development" => new UpsertProductPlanRequest("Padrão", null, 0m, 1000m, null, null, 30m),
            "revenue_share" => new UpsertProductPlanRequest("Padrão", null, 0m, null, null, 5m, 30m),
            _ => throw new ArgumentOutOfRangeException(nameof(salesStrategy), salesStrategy, "Estratégia não suportada no teste.")
        };
}
