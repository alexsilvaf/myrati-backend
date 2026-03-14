using Myrati.Application.Contracts;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class UsersServiceTests
{
    [Fact]
    public async Task GetUsersAsync_ReturnsAllUsers()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = new UsersService(scope.Context);

        var result = await service.GetUsersAsync(new UserDirectoryQuery(null, null, null));

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetUsersAsync_FiltersUsersBySearch()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = new UsersService(scope.Context);

        var result = await service.GetUsersAsync(new UserDirectoryQuery("Carlos", null, null));

        Assert.All(result, user =>
            Assert.True(
                user.Name.Contains("Carlos", StringComparison.OrdinalIgnoreCase) ||
                user.Email.Contains("Carlos", StringComparison.OrdinalIgnoreCase) ||
                user.ClientName.Contains("Carlos", StringComparison.OrdinalIgnoreCase) ||
                user.ProductName.Contains("Carlos", StringComparison.OrdinalIgnoreCase)));
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetUsersAsync_FiltersByStatus()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var service = new UsersService(scope.Context);

        var result = await service.GetUsersAsync(new UserDirectoryQuery(null, "Online", null));

        Assert.NotEmpty(result);
        Assert.All(result, user => Assert.Equal("Online", user.Status));
    }
}
