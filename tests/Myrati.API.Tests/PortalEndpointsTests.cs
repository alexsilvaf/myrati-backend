using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Myrati.API.Tests.Support;
using Myrati.Application.Abstractions;
using Myrati.Application.Contracts;
using Myrati.Domain.Clients;
using Myrati.Domain.Identity;
using Myrati.Domain.Products;
using Myrati.Infrastructure.Persistence;
using Xunit;

namespace Myrati.API.Tests;

public sealed class PortalEndpointsTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task PortalEndpoints_ReturnRealClientSnapshotAndUsers()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var numericSuffix = DateTime.UtcNow.Ticks.ToString()[^8..];
        var portalPassword = "Myrati@123";
        var portalEmail = $"portal-{suffix}@cliente.com";
        var clientId = $"CLI-PORTAL-{suffix}";
        var licenseId = $"PORT-{suffix[..4]}-{suffix[4..8]}";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            context.AddRange(
                new Client
                {
                    Id = clientId,
                    Name = $"Cliente Portal {suffix}",
                    Email = portalEmail,
                    Phone = "(11) 98888-0000",
                    Document = $"98.{numericSuffix[..3]}.{numericSuffix[3..6]}/0001-{numericSuffix[6..8]}",
                    DocumentType = "CNPJ",
                    Company = $"Portal Company {suffix}",
                    JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "Ativo"
                },
                new License
                {
                    Id = licenseId,
                    ClientId = clientId,
                    ProductId = "PRD-001",
                    Plan = "Enterprise",
                    MaxUsers = 20,
                    ActiveUsers = 6,
                    Status = "Ativa",
                    StartDate = new DateOnly(2026, 1, 10),
                    ExpiryDate = new DateOnly(2027, 1, 10),
                    MonthlyValue = 2490m
                },
                new ConnectedUser
                {
                    Id = $"USR-PORTAL-{suffix}-01",
                    ClientId = clientId,
                    ProductId = "PRD-001",
                    Name = $"Usuário Portal {suffix} A",
                    Email = portalEmail,
                    LastActiveDisplay = "Agora",
                    Status = "Online"
                },
                new ConnectedUser
                {
                    Id = $"USR-PORTAL-{suffix}-02",
                    ClientId = clientId,
                    ProductId = "PRD-001",
                    Name = $"Usuário Portal {suffix} B",
                    Email = $"time-{suffix}@cliente.com",
                    LastActiveDisplay = "15 min atrás",
                    Status = "Offline"
                },
                new AdminUser
                {
                    Id = $"TM-PORTAL-{suffix}",
                    Name = $"Portal {suffix}",
                    Email = portalEmail,
                    Role = "Cliente",
                    Status = "Ativo",
                    PasswordHash = passwordHasher.Hash(portalPassword)
                });

            await context.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsync(portalEmail, portalPassword);
        client.UseBearerToken(auth.AccessToken);

        var meResponse = await client.GetAsync("/api/v1/portal/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<PortalMeDto>();

        var usersResponse = await client.GetAsync($"/api/v1/portal/licenses/{licenseId}/users");
        usersResponse.EnsureSuccessStatusCode();
        var users = await usersResponse.Content.ReadFromJsonAsync<IReadOnlyCollection<UserDirectoryItemDto>>();

        Assert.NotNull(me);
        Assert.Equal(clientId, me.Id);
        Assert.Equal(portalEmail, me.Email);
        Assert.Equal($"Portal Company {suffix}", me.Company);
        Assert.Single(me.Licenses);
        Assert.Equal(licenseId, me.Licenses.Single().Id);
        Assert.Equal("PRD-001", me.Licenses.Single().ProductId);

        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Contains(users, user => user.Email == portalEmail);
        Assert.All(users, user => Assert.Equal(clientId, user.ClientId));
    }
}
