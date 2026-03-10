using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ClientLifecycleEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ClientLifecycle_ReturnsBusinessConflictAndAllowsDeletionAfterCleanup()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var createClientResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Cliente Teste {suffix}",
                $"cliente-{suffix}@myrati.com",
                "(11) 99999-0000",
                $"DOC-{suffix}",
                "CPF",
                $"Empresa {suffix}",
                "Ativo"));
        Assert.Equal(HttpStatusCode.Created, createClientResponse.StatusCode);

        var createdClient = await createClientResponse.Content.ReadFromJsonAsync<ClientDetailDto>();
        Assert.NotNull(createdClient);

        var updateClientResponse = await client.PutAsJsonAsync(
            $"/api/v1/backoffice/clients/{createdClient.Id}",
            new UpdateClientRequest(
                $"Cliente Editado {suffix}",
                $"cliente-editado-{suffix}@myrati.com",
                "(11) 98888-0000",
                $"DOC-UP-{suffix}",
                "CPF",
                $"Empresa Editada {suffix}",
                "Ativo"));
        updateClientResponse.EnsureSuccessStatusCode();

        var updatedClient = await updateClientResponse.Content.ReadFromJsonAsync<ClientDetailDto>();
        Assert.NotNull(updatedClient);
        Assert.Equal($"Empresa Editada {suffix}", updatedClient.Company);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createLicenseResponse = await client.PostAsJsonAsync(
            "/api/v1/backoffice/products/PRD-001/licenses",
            new CreateLicenseRequest(
                createdClient.Id,
                "Starter",
                249m,
                null,
                null,
                today.AddDays(-1).ToString("yyyy-MM-dd"),
                today.AddDays(30).ToString("yyyy-MM-dd")));
        createLicenseResponse.EnsureSuccessStatusCode();

        var createdLicense = await createLicenseResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(createdLicense);

        var deleteWithActiveLicenseResponse = await client.DeleteAsync($"/api/v1/backoffice/clients/{createdClient.Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteWithActiveLicenseResponse.StatusCode);

        var conflictPayload = await deleteWithActiveLicenseResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(conflictPayload);
        Assert.Equal("Não é possível remover um cliente com licenças ativas.", conflictPayload.Detail);

        var deleteLicenseResponse = await client.DeleteAsync($"/api/v1/backoffice/licenses/{createdLicense.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteLicenseResponse.StatusCode);

        var deleteClientResponse = await client.DeleteAsync($"/api/v1/backoffice/clients/{createdClient.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteClientResponse.StatusCode);

        var getDeletedClientResponse = await client.GetAsync($"/api/v1/backoffice/clients/{createdClient.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedClientResponse.StatusCode);
    }
}
