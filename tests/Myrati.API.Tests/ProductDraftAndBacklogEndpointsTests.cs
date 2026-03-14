using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Common;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductDraftAndBacklogEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CreateProduct_AllowsDraftPlanForDevelopmentStatus()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Draft {suffix}",
                "Cadastro inicial em modo rascunho.",
                "Descoberta",
                "Em desenvolvimento",
                "development",
                [
                    new UpsertProductPlanRequest("Descoberta", null, 0m, null, null, null, null)
                ]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);

        var createdProduct = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);
        Assert.Equal("Em desenvolvimento", createdProduct.Status);
        Assert.Equal("0.0", createdProduct.Version);
        Assert.Null(Assert.Single(createdProduct.Plans).MaxUsers);
    }

    [Fact]
    public async Task CreateProduct_AllowsHighMaintenanceMarginAndOptionalMaintenanceFields()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products",
            new CreateProductRequest(
                $"Produto Margem {suffix}",
                "Produto com margem de manutencao acima de 100%.",
                "Servicos",
                "Ativo",
                "development",
                [
                    new UpsertProductPlanRequest("Enterprise", null, 0m, 15000m, null, null, 1000m)
                ]));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdProduct = await response.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);

        var plan = Assert.Single(createdProduct.Plans);
        Assert.Equal(15000m, plan.DevelopmentCost);
        Assert.Null(plan.MaintenanceCost);
        Assert.Equal(1000m, plan.MaintenanceProfitMargin);
    }

    [Fact]
    public async Task ProductSetupAndBacklogImport_RequiresStrictSprintRangesAndMergesDuplicates()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var startDate = ApplicationTime.LocalToday();
        var endDate = startDate.AddDays(1);
        var startDateIso = startDate.ToString("yyyy-MM-dd");
        var endDateIso = endDate.ToString("yyyy-MM-dd");

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var setupResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products/setup",
            new CreateProductSetupRequest(
                new CreateProductRequest(
                    $"Produto Setup {suffix}",
                    "Produto criado pelo assistente inicial.",
                    "Operações",
                    "Em desenvolvimento",
                    "subscription",
                    [
                        new UpsertProductPlanRequest("Starter", null, 0m, null, null, null, null)
                    ]),
                new ImportProductBacklogRequest(
                    false,
                    [
                        new ImportProductSprintRequest(
                            "Sprint 0",
                            startDateIso,
                            endDateIso,
                            "Ativa",
                            [
                                new ImportProductTaskRequest(
                                    "Mapear requisitos",
                                    "Importado pelo assistente inicial.",
                                    "backlog",
                                    "medium",
                                    string.Empty,
                                    ["setup"])
                            ])
                    ])));

        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);
        Assert.Null(setupResponse.Headers.Location);

        var createdProduct = await setupResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(createdProduct);
        Assert.Equal("0.1", createdProduct.Version);
        Assert.Single(createdProduct.Kanban.Sprints);
        Assert.Single(createdProduct.Kanban.Tasks);
        Assert.Equal(startDateIso, createdProduct.Kanban.Sprints.First().StartDate);
        Assert.Equal(endDateIso, createdProduct.Kanban.Sprints.First().EndDate);

        var importRequest = new ImportProductBacklogRequest(
            true,
            [
                new ImportProductSprintRequest(
                    "Sprint 0",
                    startDateIso,
                    endDateIso,
                    "Concluída",
                    [
                        new ImportProductTaskRequest(
                            "Mapear requisitos",
                            "Duplicata que deve ser ignorada.",
                            "done",
                            "medium",
                            string.Empty,
                            ["setup"]),
                        new ImportProductTaskRequest(
                            "Fechar checklist",
                            "Nova tarefa importada em lote.",
                            "done",
                            "high",
                            string.Empty,
                            ["import"])
                    ])
            ]);

        var importResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{createdProduct.Id}/backlog/import",
            importRequest);
        importResponse.EnsureSuccessStatusCode();

        var importResult = await importResponse.Content.ReadFromJsonAsync<ProductBacklogImportResultDto>();
        Assert.NotNull(importResult);
        Assert.Equal(0, importResult.CreatedSprints);
        Assert.Equal(1, importResult.ReusedSprints);
        Assert.Equal(1, importResult.CreatedTasks);
        Assert.Equal(1, importResult.SkippedTasks);

        var productResponse = await client.GetAsync($"/api/v1/backoffice/products/{createdProduct.Id}");
        productResponse.EnsureSuccessStatusCode();

        var updatedProduct = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.NotNull(updatedProduct);
        Assert.Equal("0.1", updatedProduct.Version);
        Assert.Single(updatedProduct.Kanban.Sprints);
        Assert.Equal(2, updatedProduct.Kanban.Tasks.Count);
        Assert.Equal("Concluída", updatedProduct.Kanban.Sprints.First().Status);
    }
}
