using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Common;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductAndLicenseLifecycleEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ProductAndLicenseLifecycle_CompletesSuccessfully()
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
                $"Produto Teste {suffix}",
                "Produto criado pela suíte de integração.",
                "Operações",
                "Ativo",
                "subscription",
                [
                    new UpsertProductPlanRequest("Starter", 5, 149m, null, null, null, null),
                    new UpsertProductPlanRequest("Scale", 15, 399m, null, null, null, null)
                ]));
        Assert.Equal(HttpStatusCode.Created, createProductResponse.StatusCode);
        Assert.Null(createProductResponse.Headers.Location);

        var createdProduct = await createProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);
        Assert.Equal("0.0", createdProduct.Version);
        Assert.Equal(2, createdProduct.Plans.Count);
        var today = ApplicationTime.LocalToday();

        var updateProductResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}",
            new UpdateProductRequest(
                $"{createdProduct.Name} Atualizado",
                "Produto atualizado pela suíte de integração.",
                "Financeiro",
                "Em desenvolvimento",
                "development",
                [
                    new UpsertProductPlanRequest("Starter", 10, 199m, 12000m, 199m, null, 30m),
                    new UpsertProductPlanRequest("Scale", 30, 599m, 24000m, 599m, null, 35m)
                ]));
        updateProductResponse.EnsureSuccessStatusCode();

        var updatedProduct = await updateProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(updatedProduct);
        Assert.Equal("0.0", updatedProduct.Version);
        Assert.Equal("Em desenvolvimento", updatedProduct.Status);
        Assert.Equal("development", updatedProduct.SalesStrategy);

        var createSprintResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/sprints",
            new CreateProductSprintRequest(
                "Sprint 1 - Release",
                today.ToString("yyyy-MM-dd"),
                today.AddDays(14).ToString("yyyy-MM-dd"),
                "Ativa"));
        createSprintResponse.EnsureSuccessStatusCode();

        var productAfterSprintResponse = await client.GetAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        productAfterSprintResponse.EnsureSuccessStatusCode();
        var productAfterSprint = await productAfterSprintResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(productAfterSprint);
        Assert.Equal("0.1", productAfterSprint.Version);

        var deployResponse = await client.PostAsync($"/api/v1/backoffice/products/{createdProduct.Id}/deployments", null);
        deployResponse.EnsureSuccessStatusCode();
        var deployedProduct = await deployResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(deployedProduct);
        Assert.Equal(1, deployedProduct.ProductionDeploys);
        Assert.Equal(0, deployedProduct.DevSprintsSinceLastDeploy);
        Assert.Equal("1.0", deployedProduct.Version);
        var createLicenseResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/licenses",
            new CreateLicenseRequest(
                "CLI-001",
                "Starter",
                199m,
                12000m,
                null,
                today.AddDays(-2).ToString("yyyy-MM-dd"),
                today.AddDays(30).ToString("yyyy-MM-dd")));
        createLicenseResponse.EnsureSuccessStatusCode();

        var createdLicense = await createLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(createdLicense);
        Assert.Equal(createdProduct.Id, createdLicense.ProductId);
        Assert.Equal("Starter", createdLicense.Plan);
        Assert.Equal(12000m, createdLicense.DevelopmentCost);

        var updateLicenseResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/licenses/{createdLicense.Id}",
            new UpdateLicenseRequest(
                "CLI-001",
                "Scale",
                599m,
                24000m,
                null,
                today.AddDays(-1).ToString("yyyy-MM-dd"),
                today.AddDays(60).ToString("yyyy-MM-dd")));
        updateLicenseResponse.EnsureSuccessStatusCode();

        var updatedLicense = await updateLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(updatedLicense);
        Assert.Equal("Scale", updatedLicense.Plan);
        Assert.Equal(599m, updatedLicense.MonthlyValue);
        Assert.Equal(24000m, updatedLicense.DevelopmentCost);

        var suspendResponse = await client.PostAsync($"/api/v1/backoffice/licenses/{createdLicense.Id}/suspend", null);
        suspendResponse.EnsureSuccessStatusCode();
        var suspendedLicense = await suspendResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(suspendedLicense);
        Assert.Equal("Suspensa", suspendedLicense.Status);

        var reactivateResponse = await client.PostAsync($"/api/v1/backoffice/licenses/{createdLicense.Id}/reactivate", null);
        reactivateResponse.EnsureSuccessStatusCode();
        var reactivatedLicense = await reactivateResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(reactivatedLicense);
        Assert.Equal("Ativa", reactivatedLicense.Status);

        var deleteLicenseResponse = await client.DeleteAsync($"/api/v1/backoffice/licenses/{createdLicense.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteLicenseResponse.StatusCode);

        var deleteProductResponse = await client.DeleteAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteProductResponse.StatusCode);

        var deletedProductResponse = await client.GetAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedProductResponse.StatusCode);

        var deletedProductError = await deletedProductResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(deletedProductError);
        Assert.Contains(createdProduct.Id, deletedProductError.Detail, StringComparison.Ordinal);
    }
}
