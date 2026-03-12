using Myrati.Application.Abstractions;

namespace Myrati.API.Tests.Support;

public sealed class TestContactLeadEmailSender : IContactLeadEmailSender
{
    public Task SendAsync(ContactLeadEmailMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public void Reset() { }
}
