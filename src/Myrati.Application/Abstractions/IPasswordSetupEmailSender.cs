namespace Myrati.Application.Abstractions;

public interface IPasswordSetupEmailSender
{
    Task SendAsync(
        string recipientName,
        string recipientEmail,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
