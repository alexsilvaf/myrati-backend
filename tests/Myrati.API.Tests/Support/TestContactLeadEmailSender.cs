using Myrati.Application.Abstractions;

namespace Myrati.API.Tests.Support;

public sealed class TestContactLeadEmailSender : IContactLeadEmailSender
{
    private readonly List<ContactLeadEmailMessage> _messages = [];
    private readonly object _sync = new();

    public IReadOnlyCollection<ContactLeadEmailMessage> Messages
    {
        get
        {
            lock (_sync)
            {
                return _messages.ToArray();
            }
        }
    }

    public Task SendAsync(ContactLeadEmailMessage message, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _messages.Add(message);
        }

        return Task.CompletedTask;
    }

    public ContactLeadEmailMessage? FindByLeadEmail(string leadEmail)
    {
        lock (_sync)
        {
            return _messages.LastOrDefault(x => string.Equals(x.Email, leadEmail, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Reset()
    {
        lock (_sync)
        {
            _messages.Clear();
        }
    }
}
