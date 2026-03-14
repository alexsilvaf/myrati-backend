using Moq;
using Myrati.Application.Abstractions;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class PortalServiceTests
{
    [Fact]
    public async Task GetPortalMeAsync_ReturnsPortalInfoForClient()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.Email).Returns("carlos@techcorp.com.br");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new PortalService(scope.Context, userContext.Object);

        var result = await service.GetPortalMeAsync();

        Assert.Equal("CLI-001", result.Id);
        Assert.Equal("Carlos Silva", result.Name);
        Assert.Equal("carlos@techcorp.com.br", result.Email);
        Assert.Equal("TechCorp Brasil", result.Company);
    }

    [Fact]
    public async Task GetLicenseUsersAsync_WithInvalidLicenseId_ThrowsEntityNotFoundException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var userContext = new Mock<ICurrentUserContext>();
        userContext.Setup(x => x.Email).Returns("carlos@techcorp.com.br");
        userContext.Setup(x => x.IsAuthenticated).Returns(true);
        var service = new PortalService(scope.Context, userContext.Object);

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => service.GetLicenseUsersAsync("LIC-INVALID"));
    }
}
