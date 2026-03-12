using Myrati.Application.Abstractions;

namespace Myrati.API.Tests.Support;

public sealed class TestPasswordSetupEmailSender : IPasswordSetupEmailSender
{
    private readonly List<SentPasswordSetupEmail> _emails = [];
    private readonly object _sync = new();

    public IReadOnlyCollection<SentPasswordSetupEmail> Emails
    {
        get
        {
            lock (_sync)
            {
                return _emails.ToArray();
            }
        }
    }

    public Task SendAsync(
        string recipientName,
        string recipientEmail,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _emails.Add(new SentPasswordSetupEmail(recipientName, recipientEmail, token, expiresAt));
        }

        return Task.CompletedTask;
    }

    public SentPasswordSetupEmail? FindByEmail(string recipientEmail)
    {
        lock (_sync)
        {
            return _emails.LastOrDefault(x => string.Equals(x.RecipientEmail, recipientEmail, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _emails.Clear();
        }
    }
}

public sealed record SentPasswordSetupEmail(
    string RecipientName,
    string RecipientEmail,
    string Token,
    DateTimeOffset ExpiresAt);
