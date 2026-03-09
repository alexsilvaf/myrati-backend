using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Infrastructure.Security;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ProfileServiceTests
{
    [Fact]
    public async Task ChangePasswordAsync_WithInvalidCurrentPassword_ThrowsUnauthorizedAccessException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new ProfileService(
            scope.Context,
            new PasswordHasher(),
            new UpdateProfileRequestValidator(),
            new ChangePasswordRequestValidator(),
            publisher);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ChangePasswordAsync(
                "admin@myrati.com",
                new("senha-incorreta", "NovaSenha123", "NovaSenha123")));

        Assert.Empty(publisher.Events);
    }
}
