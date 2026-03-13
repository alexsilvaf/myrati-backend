using Myrati.Application.Common.Exceptions;
using Myrati.Application.Services;
using Myrati.Application.Tests.Support;
using Myrati.Application.Validation;
using Myrati.Infrastructure.Security;
using Xunit;

namespace Myrati.Application.Tests;

public sealed class ClientsServiceTests
{
    [Fact]
    public async Task CreateClientAsync_CreatesPortalAccessAndPasswordSetupInvitation()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var emailSender = new TestPasswordSetupEmailSender();
        var service = new ClientsService(
            scope.Context,
            new PasswordHasher(),
            emailSender,
            new CreateClientRequestValidator(),
            new UpdateClientRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher());

        var response = await service.CreateClientAsync(
            new(
                "Cliente Portal",
                "cliente.portal@empresa.com",
                "(11) 99999-1234",
                "123.456.789-00",
                "CPF",
                "Empresa Portal",
                "Ativo"));

        Assert.Equal("cliente.portal@empresa.com", response.Email);
        Assert.NotNull(emailSender.FindByEmail("cliente.portal@empresa.com"));
        var createdPortalUser = Assert.Single(
            scope.Context.AdminUsersSet,
            user => user.Email == "cliente.portal@empresa.com" && user.Role == "Cliente");
        Assert.Contains(scope.Context.PasswordSetupTokensSet, token => token.AdminUserId == createdPortalUser.Id);
        Assert.Contains(publisher.Events, x => x.EventType == "client.created");
    }

    [Fact]
    public async Task DeleteClientAsync_WithActiveLicense_ThrowsConflictException()
    {
        await using var scope = await SeededDbContextScope.CreateAsync();
        var publisher = new TestRealtimeEventPublisher();
        var service = new ClientsService(
            scope.Context,
            new PasswordHasher(),
            new TestPasswordSetupEmailSender(),
            new CreateClientRequestValidator(),
            new UpdateClientRequestValidator(),
            publisher,
            new TestBackofficeNotificationPublisher());

        await Assert.ThrowsAsync<ConflictException>(() => service.DeleteClientAsync("CLI-001"));
        Assert.Empty(publisher.Events);
    }
}

file sealed class TestPasswordSetupEmailSender : Myrati.Application.Abstractions.IPasswordSetupEmailSender
{
    private readonly List<(string RecipientName, string RecipientEmail, string Token, DateTimeOffset ExpiresAt)> _emails = [];

    public Task SendAsync(
        string recipientName,
        string recipientEmail,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        _emails.Add((recipientName, recipientEmail, token, expiresAt));
        return Task.CompletedTask;
    }

    public (string RecipientName, string RecipientEmail, string Token, DateTimeOffset ExpiresAt)? FindByEmail(string recipientEmail) =>
        _emails.LastOrDefault(email => string.Equals(email.RecipientEmail, recipientEmail, StringComparison.OrdinalIgnoreCase));
}
