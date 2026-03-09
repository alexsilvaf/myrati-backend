using System.Net;
using System.Net.Http.Json;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class PublicEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task StatusEndpoint_ReturnsSnapshot()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/public/status");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SystemStatusResponse>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Services);
        Assert.NotEmpty(payload.UptimeHistory);
    }

    [Fact]
    public async Task ContactEndpoint_WithInvalidPayload_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/contact",
            new ContactRequest("", "email-invalido", "", "", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
