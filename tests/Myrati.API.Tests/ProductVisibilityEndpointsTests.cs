using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductVisibilityEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Developer_OnlySeesProductsWhereIsCollaborator()
    {
        using var developerClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var developerAuth = await developerClient.LoginAsync("bruno@myrati.com", "Myrati@123");
        developerClient.UseBearerToken(developerAuth.AccessToken);

        var listResponse = await developerClient.GetAsync("/api/v1/backoffice/products");
        listResponse.EnsureSuccessStatusCode();

        var products = await listResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();
        Assert.NotNull(products);

        var visibleProductIds = products
            .Select(product => product.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["PRD-001", "PRD-004"], visibleProductIds);

        var allowedDetailResponse = await developerClient.GetAsync("/api/v1/backoffice/products/PRD-001");
        Assert.Equal(HttpStatusCode.OK, allowedDetailResponse.StatusCode);

        var forbiddenDetailResponse = await developerClient.GetAsync("/api/v1/backoffice/products/PRD-002");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDetailResponse.StatusCode);

        var forbiddenKanbanResponse = await developerClient.GetAsync("/api/v1/backoffice/products/PRD-002/kanban");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenKanbanResponse.StatusCode);
    }

    [Fact]
    public async Task DeveloperWhoCreatesProduct_GainsFullAccessToIt()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var creatorClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var creatorAuth = await creatorClient.LoginAsync("bruno@myrati.com", "Myrati@123");
        creatorClient.UseBearerToken(creatorAuth.AccessToken);

        var createResponse = await creatorClient.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Autoral {suffix}",
                "Produto criado por desenvolvedor para validar acesso total do criador.",
                "Laboratório",
                "Em desenvolvimento",
                "subscription",
                "1.0.0",
                [new UpsertProductPlanRequest("Core", 15, 249m, null, null, null)]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);

        var creatorCollaborator = Assert.Single(createdProduct.Collaborators, collaborator => collaborator.MemberId == "TM-005");
        Assert.True(creatorCollaborator.Permissions.Tasks.Delete);
        Assert.True(creatorCollaborator.Permissions.Sprints.Delete);
        Assert.True(creatorCollaborator.Permissions.Licenses.Delete);
        Assert.True(creatorCollaborator.Permissions.Product.Delete);

        var updateResponse = await creatorClient.PutAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}",
            new UpdateProductRequest(
                $"{createdProduct.Name} Atualizado",
                createdProduct.Description,
                createdProduct.Category,
                createdProduct.Status,
                createdProduct.SalesStrategy,
                "1.0.1",
                [new UpsertProductPlanRequest("Core", 20, 299m, null, null, null)]));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var listResponse = await creatorClient.GetAsync("/api/v1/backoffice/products");
        listResponse.EnsureSuccessStatusCode();

        var visibleProducts = await listResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();
        Assert.NotNull(visibleProducts);
        Assert.Contains(visibleProducts, product => product.Id == createdProduct.Id);

        using var otherDeveloperClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var otherDeveloperAuth = await otherDeveloperClient.LoginAsync("joao@myrati.com", "Myrati@123");
        otherDeveloperClient.UseBearerToken(otherDeveloperAuth.AccessToken);

        var otherDeveloperDetailResponse = await otherDeveloperClient.GetAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.Forbidden, otherDeveloperDetailResponse.StatusCode);

        var deleteResponse = await creatorClient.DeleteAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
