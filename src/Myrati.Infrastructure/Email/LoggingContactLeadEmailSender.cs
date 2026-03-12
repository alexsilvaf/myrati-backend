using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Myrati.Application.Abstractions;

namespace Myrati.Infrastructure.Email;

internal sealed class LoggingContactLeadEmailSender(
    IConfiguration configuration,
    ILogger<LoggingContactLeadEmailSender> logger) : IContactLeadEmailSender
{
    private readonly ContactLeadEmailOptions _options = ContactLeadEmailOptions.FromConfiguration(configuration);

    public Task SendAsync(ContactLeadEmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogWarning(
            """
            Envio de lead por e-mail nao configurado. Lead {LeadId} destinado a {RecipientEmail}: Nome={LeadName}; Email={LeadEmail}; Empresa={Company}; Assunto={Subject}; Mensagem={Body}
            """,
            message.LeadId,
            _options.LeadRecipientEmail,
            message.Name,
            message.Email,
            string.IsNullOrWhiteSpace(message.Company) ? "Nao informado" : message.Company,
            string.IsNullOrWhiteSpace(message.Subject) ? "Sem assunto" : message.Subject,
            message.Message);

        return Task.CompletedTask;
    }
}
