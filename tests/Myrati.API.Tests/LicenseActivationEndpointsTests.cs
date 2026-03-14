using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.Application.Common;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class LicenseActivationEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task ActivateEndpoint_WithMatchingProductAndLicense_ReturnsOk()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var createdLicense = await CreateLicenseAsync(client, "PRD-001", "CLI-001", "Starter");

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/licenses/activate",
            new LicenseActivationRequest("PRD-001", createdLicense.Id));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LicenseActivationResponse>();
        Assert.NotNull(payload);
        Assert.Equal(createdLicense.Id, payload.LicenseId);
        Assert.Equal("PRD-001", payload.ProductId);
        Assert.Equal("Ativa", payload.Status);
    }

    [Fact]
    public async Task ActivateEndpoint_WithLicenseFromAnotherProduct_ReturnsConflict()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var createdLicense = await CreateLicenseAsync(client, "PRD-001", "CLI-001", "Starter");

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/licenses/activate",
            new LicenseActivationRequest("PRD-002", createdLicense.Id));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("não pertence ao produto solicitado", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<LicenseDto> CreateLicenseAsync(
        HttpClient client,
        string productId,
        string clientId,
        string plan)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@myrati.com", "Myrati@123"));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var today = ApplicationTime.LocalToday();
        var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/backoffice/products/{productId}/licenses",
            new CreateLicenseRequest(
                clientId,
                plan,
                990m,
                null,
                null,
                today.AddDays(-1).ToString("yyyy-MM-dd"),
                today.AddDays(30).ToString("yyyy-MM-dd")));

        createResponse.EnsureSuccessStatusCode();

        var license = await createResponse.Content.ReadFromJsonAsync<LicenseDto>();
        Assert.NotNull(license);
        return license;
    }
}
