using Microsoft.Extensions.Configuration;
using Myrati.Application.Tests.Support;
using Myrati.Infrastructure.Security;
using Myrati.Infrastructure.Seeding;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ProductionSuperAdminPasswordSetupBootstrapperTests
{
    [Fact]
    public async Task SendInvitationsAsync_WithSeededProductionAdmins_CreatesPendingInvitations()
    {
        await using var scope = await SeededDbContextScope.CreateAsync(seed: false);
        var passwordHasher = new PasswordHasher();
        var seeder = new MyratiDbSeeder(passwordHasher, BuildProductionSeedConfiguration());

        await seeder.SeedAsync(scope.Context);

        var emailSender = new RecordingPasswordSetupEmailSender();
        var bootstrapper = new ProductionSuperAdminPasswordSetupBootstrapper(passwordHasher, emailSender);

        await bootstrapper.SendInvitationsAsync(scope.Context);

        Assert.Equal(2, emailSender.Emails.Count);
        Assert.NotNull(emailSender.FindByEmail("alex@myrati.com.br"));
        Assert.NotNull(emailSender.FindByEmail("yasmin@myrati.com.br"));

        var alex = Assert.Single(scope.Context.AdminUsersSet, user => user.Email == "alex@myrati.com.br");
        var yasmin = Assert.Single(scope.Context.AdminUsersSet, user => user.Email == "yasmin@myrati.com.br");
        Assert.Equal("Convite Pendente", alex.Status);
        Assert.Equal("Convite Pendente", yasmin.Status);
        Assert.False(passwordHasher.Verify("Myrati@123", alex.PasswordHash));
        Assert.False(passwordHasher.Verify("Myrati@123", yasmin.PasswordHash));
        Assert.Contains(scope.Context.PasswordSetupTokensSet, token => token.AdminUserId == alex.Id);
        Assert.Contains(scope.Context.PasswordSetupTokensSet, token => token.AdminUserId == yasmin.Id);
    }

    [Fact]
    public async Task SendInvitationsAsync_WithActiveInvitation_DoesNotResend()
    {
        await using var scope = await SeededDbContextScope.CreateAsync(seed: false);
        var passwordHasher = new PasswordHasher();
        var seeder = new MyratiDbSeeder(passwordHasher, BuildProductionSeedConfiguration());

        await seeder.SeedAsync(scope.Context);

        var emailSender = new RecordingPasswordSetupEmailSender();
        var bootstrapper = new ProductionSuperAdminPasswordSetupBootstrapper(passwordHasher, emailSender);

        await bootstrapper.SendInvitationsAsync(scope.Context);
        emailSender.Reset();

        await bootstrapper.SendInvitationsAsync(scope.Context);

        Assert.Empty(emailSender.Emails);
        Assert.Equal(
            2,
            scope.Context.PasswordSetupTokensSet
                .AsEnumerable()
                .Count(token => token.UsedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow));
    }

    private static IConfiguration BuildProductionSeedConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Seeding:IncludeDemoData"] = "false"
            })
            .Build();

    private sealed class RecordingPasswordSetupEmailSender : Myrati.Application.Abstractions.IPasswordSetupEmailSender
    {
        private readonly List<SentPasswordSetupEmail> _emails = [];

        public IReadOnlyList<SentPasswordSetupEmail> Emails => _emails;

        public Task SendAsync(
            string recipientName,
            string recipientEmail,
            string token,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            _emails.Add(new SentPasswordSetupEmail(recipientName, recipientEmail, token, expiresAt));
            return Task.CompletedTask;
        }

        public SentPasswordSetupEmail? FindByEmail(string recipientEmail) =>
            _emails.LastOrDefault(email => string.Equals(email.RecipientEmail, recipientEmail, StringComparison.OrdinalIgnoreCase));

        public void Reset() => _emails.Clear();
    }

    private sealed record SentPasswordSetupEmail(
        string RecipientName,
        string RecipientEmail,
        string Token,
        DateTimeOffset ExpiresAt);
}
