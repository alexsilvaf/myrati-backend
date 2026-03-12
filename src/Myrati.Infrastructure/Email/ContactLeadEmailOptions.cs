using Microsoft.Extensions.Configuration;

namespace Myrati.Infrastructure.Email;

internal sealed class ContactLeadEmailOptions
{
    public string SenderName { get; init; } = "Myrati";
    public string SenderEmail { get; init; } = string.Empty;
    public string GmailClientId { get; init; } = string.Empty;
    public string GmailClientSecret { get; init; } = string.Empty;
    public string GmailRefreshToken { get; init; } = string.Empty;
    public string LeadRecipientName { get; init; } = "Yasmin";
    public string LeadRecipientEmail { get; init; } = "yasmin@myrati.com.br";

    public bool HasGmailConfiguration =>
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(GmailClientId) &&
        !string.IsNullOrWhiteSpace(GmailClientSecret) &&
        !string.IsNullOrWhiteSpace(GmailRefreshToken) &&
        !string.IsNullOrWhiteSpace(LeadRecipientEmail);

    public static ContactLeadEmailOptions FromConfiguration(IConfiguration configuration) =>
        new()
        {
            SenderName = configuration["Email:SenderName"] ?? "Myrati",
            SenderEmail = configuration["Email:SenderEmail"] ?? string.Empty,
            GmailClientId = configuration["Email:Gmail:ClientId"] ?? string.Empty,
            GmailClientSecret = configuration["Email:Gmail:ClientSecret"] ?? string.Empty,
            GmailRefreshToken = configuration["Email:Gmail:RefreshToken"] ?? string.Empty,
            LeadRecipientName = configuration["Email:LeadRecipientName"] ?? "Yasmin",
            LeadRecipientEmail = configuration["Email:LeadRecipientEmail"] ?? "yasmin@myrati.com.br",
        };
}
