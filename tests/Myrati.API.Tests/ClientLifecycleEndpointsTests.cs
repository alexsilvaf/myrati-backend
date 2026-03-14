using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.API.Tests.Support;
using Myrati.Application.Common;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ClientLifecycleEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ClientLifecycle_ReturnsBusinessConflictAndAllowsDeletionAfterCleanup()
    {
        factory.PasswordSetupEmailSender.Reset();
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
        Assert.NotNull(factory.PasswordSetupEmailSender.FindByEmail($"cliente-{suffix}@myrati.com"));

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

        var today = ApplicationTime.LocalToday();
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

    [Fact]
    public async Task CreateClient_SendsPasswordSetupEmailAndAllowsPortalAccess()
    {
        factory.PasswordSetupEmailSender.Reset();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var clientEmail = $"portal-{suffix}@myrati.com";
        const string portalPassword = "Portal@123";

        using var adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var adminAuth = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearerToken(adminAuth.AccessToken);

        var createClientResponse = await adminClient.PostAsJsonAsync(
            "/api/v1/backoffice/clients",
            new CreateClientRequest(
                $"Contato Portal {suffix}",
                clientEmail,
                "(11) 98888-0000",
                $"DOC-PORTAL-{suffix}",
                "CPF",
                $"Empresa Portal {suffix}",
                "Ativo"));
        Assert.Equal(HttpStatusCode.Created, createClientResponse.StatusCode);

        var invitation = factory.PasswordSetupEmailSender.FindByEmail(clientEmail);
        Assert.NotNull(invitation);

        using var publicClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var passwordSetupSessionResponse = await publicClient.GetAsync(
            $"/api/v1/auth/password-setup?token={Uri.EscapeDataString(invitation.Token)}");
        passwordSetupSessionResponse.EnsureSuccessStatusCode();

        var passwordSetupSession = await passwordSetupSessionResponse.Content.ReadFromJsonAsync<PasswordSetupSessionDto>();
        Assert.NotNull(passwordSetupSession);
        Assert.Equal(clientEmail, passwordSetupSession.Email);

        var completePasswordSetupResponse = await publicClient.PostAsJsonAsync(
            "/api/v1/auth/password-setup",
            new PasswordSetupRequest(invitation.Token, portalPassword, portalPassword));
        Assert.Equal(HttpStatusCode.NoContent, completePasswordSetupResponse.StatusCode);

        var loginResponse = await publicClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(clientEmail, portalPassword));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal("Cliente", auth.User.Role);
        publicClient.UseBearerToken(auth.AccessToken);

        var portalResponse = await publicClient.GetAsync("/api/v1/portal/me");
        portalResponse.EnsureSuccessStatusCode();

        var portalMe = await portalResponse.Content.ReadFromJsonAsync<PortalMeDto>();
        Assert.NotNull(portalMe);
        Assert.Equal(clientEmail, portalMe.Email);
        Assert.Equal($"Empresa Portal {suffix}", portalMe.Company);
    }
}
