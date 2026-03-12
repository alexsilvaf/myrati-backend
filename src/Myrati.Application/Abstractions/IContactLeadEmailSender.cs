namespace Myrati.Application.Abstractions;

public interface IContactLeadEmailSender
{
    Task SendAsync(
        ContactLeadEmailMessage message,
        CancellationToken cancellationToken = default);
}
