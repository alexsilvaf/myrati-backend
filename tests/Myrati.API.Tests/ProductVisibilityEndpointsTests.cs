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
                [new UpsertProductPlanRequest("Core", 15, 249m, null, null, null, null)]));
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
                [new UpsertProductPlanRequest("Core", 20, 299m, null, null, null, null)]));
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

    [Fact]
    public async Task DeveloperWithoutPlanView_DoesNotSeeRevenueOrPlansOnProductAndDashboard()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var adminAuth = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearerToken(adminAuth.AccessToken);

        var restrictedProductResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Restrito {suffix}",
                "Produto com receita oculta para colaborador sem permissão comercial.",
                "QA",
                "Ativo",
                "subscription",
                [new UpsertProductPlanRequest("Professional", 10, 100m, null, null, null, null)]));
        Assert.Equal(HttpStatusCode.Created, restrictedProductResponse.StatusCode);

        var restrictedProduct = await restrictedProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(restrictedProduct);

        var privateProductResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Privado {suffix}",
                "Produto fora do escopo do desenvolvedor.",
                "QA",
                "Ativo",
                "subscription",
                [new UpsertProductPlanRequest("Enterprise", 20, 900m, null, null, null, null)]));
        Assert.Equal(HttpStatusCode.Created, privateProductResponse.StatusCode);

        var privateProduct = await privateProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(privateProduct);

        var collaboratorResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{restrictedProduct.Id}/collaborators",
            new AddProductCollaboratorRequest(
                "TM-005",
                new ProductCollaboratorPermissionsDto(
                    new ProductPermissionSetDto(true, true, true, false),
                    new ProductPermissionSetDto(true, true, true, false),
                    new ProductPermissionSetDto(true, true, true, false),
                    new ProductPermissionSetDto(false, false, false, false),
                    new ProductPermissionSetDto(true, false, false, false))));
        collaboratorResponse.EnsureSuccessStatusCode();

        var restrictedLicenseResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{restrictedProduct.Id}/licenses",
            new CreateLicenseRequest(
                "CLI-001",
                "Professional",
                100m,
                null,
                null,
                "2026-03-14",
                "2027-03-14"));
        restrictedLicenseResponse.EnsureSuccessStatusCode();
        var restrictedLicenseEntity = await restrictedLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(restrictedLicenseEntity);

        var privateLicenseResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{privateProduct.Id}/licenses",
            new CreateLicenseRequest(
                "CLI-001",
                "Enterprise",
                900m,
                null,
                null,
                "2026-03-14",
                "2027-03-14"));
        privateLicenseResponse.EnsureSuccessStatusCode();
        var privateLicenseEntity = await privateLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(privateLicenseEntity);

        using var developerClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var developerAuth = await developerClient.LoginAsync("bruno@myrati.com", "Myrati@123");
        developerClient.UseBearerToken(developerAuth.AccessToken);

        var restrictedDetailResponse = await developerClient.GetAsync($"/api/v1/backoffice/products/{restrictedProduct.Id}");
        restrictedDetailResponse.EnsureSuccessStatusCode();
        var restrictedDetail = await restrictedDetailResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.NotNull(restrictedDetail);
        Assert.False(restrictedDetail.CanViewPlans);
        Assert.Equal(0m, restrictedDetail.MonthlyRevenue);
        Assert.Empty(restrictedDetail.Plans);
        var restrictedLicense = Assert.Single(restrictedDetail.Licenses);
        Assert.Equal("Restrito", restrictedLicense.Plan);
        Assert.Equal(0m, restrictedLicense.MonthlyValue);

        var dashboardResponse = await developerClient.GetAsync("/api/v1/backoffice/dashboard");
        dashboardResponse.EnsureSuccessStatusCode();
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(dashboard);
        Assert.DoesNotContain(dashboard.RevenueByProduct, item => item.Name == restrictedProduct.Name);
        Assert.DoesNotContain(dashboard.RevenueByProduct, item => item.Name == privateProduct.Name);
        Assert.Contains(dashboard.ProductHealth, item => item.ProductId == restrictedProduct.Id && item.Revenue == 0m);
        Assert.DoesNotContain(dashboard.ProductHealth, item => item.ProductId == privateProduct.Id);

        var deleteRestrictedLicenseResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/licenses/{restrictedLicenseEntity.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRestrictedLicenseResponse.StatusCode);

        var deletePrivateLicenseResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/licenses/{privateLicenseEntity.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletePrivateLicenseResponse.StatusCode);

        var deleteRestrictedProductResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/products/{restrictedProduct.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteRestrictedProductResponse.StatusCode);

        var deletePrivateProductResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/products/{privateProduct.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deletePrivateProductResponse.StatusCode);
    }
}
