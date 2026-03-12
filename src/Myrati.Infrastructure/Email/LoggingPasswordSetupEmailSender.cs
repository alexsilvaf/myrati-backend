using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Myrati.Application.Abstractions;

namespace Myrati.Infrastructure.Email;

internal sealed class LoggingPasswordSetupEmailSender(
    IConfiguration configuration,
    ILogger<LoggingPasswordSetupEmailSender> logger) : IPasswordSetupEmailSender
{
    private readonly PasswordSetupEmailOptions _options = PasswordSetupEmailOptions.FromConfiguration(configuration);

    public Task SendAsync(
        string recipientName,
        string recipientEmail,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            "Email de convite nao configurado. Link de definicao de senha para {RecipientEmail}: {SetupUrl}. Expira em {ExpiresAt:O}",
            recipientEmail,
            _options.BuildPasswordSetupUrl(token),
            expiresAt);

        return Task.CompletedTask;
    }
}
