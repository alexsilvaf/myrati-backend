using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Myrati.Application.Contracts;
using Myrati.Infrastructure.Persistence;
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

    [Fact]
    public async Task ContactEndpoint_WithValidPayload_PersistsLead()
    {
        using var client = factory.CreateClient();
        factory.ContactLeadEmailSender.Reset();

        var response = await client.PostAsJsonAsync(
            "/api/v1/public/contact",
            new ContactRequest(
                "Contato Teste",
                "contato.teste@myrati.com",
                "Empresa Teste",
                "Comercial",
                "Gostaria de conhecer a plataforma."));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(payload);
        Assert.Contains("sucesso", payload.Message, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();

        Assert.Contains(
            dbContext.ContactLeadsSet,
            lead => lead.Email == "contato.teste@myrati.com" && lead.Company == "Empresa Teste");
        Assert.NotNull(factory.ContactLeadEmailSender.FindByLeadEmail("contato.teste@myrati.com"));
    }
}
