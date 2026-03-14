using Moq;
using Myrati.Application.Abstractions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsGlobalDashboardForAdmin()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-001");
        userContext.Setup(x => x.Email).Returns("admin@myrati.com");
        userContext.Setup(x => x.Role).Returns("Admin");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.True(result.CanViewRevenue);
        Assert.True(result.TotalLicensesCount > 0);
        Assert.NotEmpty(result.ProductHealth);
    }

    [Fact]
    public async Task GetAsync_ReturnsDeveloperDashboardForDeveloper()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.UserId).Returns("TM-003");
        userContext.Setup(x => x.Email).Returns("joao@myrati.com");
        userContext.Setup(x => x.Role).Returns("Desenvolvedor");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new DashboardService(scope.Context, userContext.Object);

        var result = await service.GetAsync();

        Assert.NotNull(result);
    }
}
