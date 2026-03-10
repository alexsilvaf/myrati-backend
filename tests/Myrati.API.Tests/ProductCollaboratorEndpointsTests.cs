using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductCollaboratorEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DeveloperPermissions_AreEnforcedPerProductCollaborator()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var adminAuth = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearerToken(adminAuth.AccessToken);

        var createProductResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Escopo {suffix}",
                "Produto para validar permissoes por colaborador.",
                "QA",
                "Em desenvolvimento",
                "subscription",
                "1.0.0",
                [new UpsertProductPlanRequest("Starter", 10, 199m, null, null, null)]));
        Assert.Equal(HttpStatusCode.Created, createProductResponse.StatusCode);

        var createdProduct = await createProductResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);

        var createSprintResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/sprints",
            new CreateProductSprintRequest("Sprint Escopo", "2026-03-10", "2026-03-24", "Ativa"));
        createSprintResponse.EnsureSuccessStatusCode();

        var createdSprint = await createSprintResponse.Content.ReadFromJsonAsync<ProductSprintDto>();
        Assert.NotNull(createdSprint);

        var addCollaboratorResponse = await adminClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/collaborators",
            new AddProductCollaboratorRequest(
                "TM-005",
                CreatePermissions(
                    tasksCreate: true,
                    tasksEdit: true,
                    licensesCreate: false,
                    licensesEdit: false,
                    productEdit: false)));
        addCollaboratorResponse.EnsureSuccessStatusCode();

        using var developerClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var developerAuth = await developerClient.LoginAsync("bruno@myrati.com", "Myrati@123");
        developerClient.UseBearerToken(developerAuth.AccessToken);

        var createTaskResponse = await developerClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/tasks",
            new CreateProductTaskRequest(
                createdSprint.Id,
                "Implementar automacao",
                "Fluxo validado por desenvolvedor colaborador.",
                "todo",
                "high",
                "Bruno Lima",
                ["backend"]));
        createTaskResponse.EnsureSuccessStatusCode();

        var forbiddenLicenseResponse = await developerClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/licenses",
            new CreateLicenseRequest(
                "CLI-001",
                "Starter",
                199m,
                null,
                null,
                "2026-03-10",
                "2026-06-10"));
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenLicenseResponse.StatusCode);

        var updateCollaboratorResponse = await adminClient.PutAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/collaborators/TM-005",
            new UpdateProductCollaboratorRequest(
                CreatePermissions(
                    tasksCreate: true,
                    tasksEdit: true,
                    licensesCreate: true,
                    licensesEdit: true,
                    productEdit: false)));
        updateCollaboratorResponse.EnsureSuccessStatusCode();

        var allowedLicenseResponse = await developerClient.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/licenses",
            new CreateLicenseRequest(
                "CLI-001",
                "Starter",
                199m,
                null,
                null,
                "2026-03-10",
                "2026-06-10"));
        allowedLicenseResponse.EnsureSuccessStatusCode();

        var createdLicense = await allowedLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(createdLicense);
        Assert.Equal(createdProduct.Id, createdLicense.ProductId);

        var deleteLicenseResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/licenses/{createdLicense.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteLicenseResponse.StatusCode);

        var deleteProductResponse = await adminClient.DeleteAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteProductResponse.StatusCode);
    }

    private static ProductCollaboratorPermissionsDto CreatePermissions(
        bool tasksCreate,
        bool tasksEdit,
        bool licensesCreate,
        bool licensesEdit,
        bool productEdit)
    {
        return new ProductCollaboratorPermissionsDto(
            new ProductPermissionSetDto(true, tasksCreate, tasksEdit, false),
            new ProductPermissionSetDto(true, false, false, false),
            new ProductPermissionSetDto(true, licensesCreate, licensesEdit, false),
            new ProductPermissionSetDto(true, false, productEdit, false));
    }
}
