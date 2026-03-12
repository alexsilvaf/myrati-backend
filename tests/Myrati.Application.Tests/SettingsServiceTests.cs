using Myrati.Application.Common.Exceptions;
using Myrati.Application.Abstractions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Infrastructure.Security;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class SettingsServiceTests
{
    [Fact]
    public async Task DeleteTeamMemberAsync_WithSuperAdmin_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new SettingsService(
            scope.Context,
            new PasswordHasher(),
            new NoOpPasswordSetupEmailSender(),
            new UpdateSettingsRequestValidator(),
            new CreateApiKeyRequestValidator(),
            new CreateTeamMemberRequestValidator(),
            new UpdateTeamMemberRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher());

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteTeamMemberAsync("TM-001"));
        Assert.Empty(publisher.Events);
    }

    private sealed class NoOpPasswordSetupEmailSender : IPasswordSetupEmailSender
    {
        public Task SendAsync(
            string recipientName,
            string recipientEmail,
            string token,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
