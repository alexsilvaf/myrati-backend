using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Myrati.Application.Contracts;
using Xunit;

namespace Myrati.API.Tests;

public sealed class AuthenticationFlowTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task LoginAndFetchProducts_ReturnsSeededBackofficeData()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@myrati.com", "Myrati@123"));
        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var productsResponse = await client.GetAsync("/api/v1/backoffice/products");
        productsResponse.EnsureSuccessStatusCode();

        var products = await productsResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<ProductSummaryDto>>();
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }
}
