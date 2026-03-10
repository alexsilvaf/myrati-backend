using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Myrati.API.Tests.Support;
using Myrati.Domain.Clients;
using Myrati.Domain.Products;
using Myrati.Infrastructure.Persistence;
using Xunit;

namespace Myrati.API.Tests;

public sealed class ProductDeletionTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task DeleteProduct_WithConnectedUsersAndNoLicenses_ReturnsNoContent()
    {
        const string productId = "PRD-900";

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<MyratiDbContext>();

            await dbContext.AddAsync(new Product
            {
                Id = productId,
                Name = "Produto Temporario",
                Description = "Produto temporario para teste de exclusao.",
                Category = "Teste",
                Status = "Ativo",
                Version = "1.0.0",
                CreatedDate = new DateOnly(2026, 3, 9)
            });
            await dbContext.AddAsync(new ProductPlan
            {
                Id = $"{productId}-PLAN-01",
                ProductId = productId,
                Name = "Starter",
                MaxUsers = 5,
                MonthlyPrice = 100m
            });
            await dbContext.AddAsync(new ConnectedUser
            {
                Id = "USR-900",
                ClientId = "CLI-001",
                ProductId = productId,
                Name = "Usuario de Teste",
                Email = "usuario-teste@myrati.com",
                LastActiveDisplay = "Agora",
                Status = "Online"
            });
            await dbContext.SaveChangesAsync();
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var auth = await client.LoginAsAdminAsync();
        client.UseBearerToken(auth.AccessToken);

        var response = await client.DeleteAsync($"/api/v1/backoffice/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<MyratiDbContext>();

        Assert.DoesNotContain(verificationDbContext.ProductsSet, product => product.Id == productId);
        Assert.DoesNotContain(verificationDbContext.ConnectedUsersSet, user => user.ProductId == productId);
    }
}
