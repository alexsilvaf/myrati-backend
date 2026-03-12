using System.Net;
using System.Net.Http;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using Myrati.Application.Abstractions;
using SaslMechanismOAuth2 = MailKit.Security.SaslMechanismOAuth2;

namespace Myrati.Infrastructure.Email;

internal sealed class GmailContactLeadEmailSender(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<GmailContactLeadEmailSender> logger) : IContactLeadEmailSender
{
    private readonly ContactLeadEmailOptions _options = ContactLeadEmailOptions.FromConfiguration(configuration);

    public async Task SendAsync(ContactLeadEmailMessage lead, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        message.To.Add(new MailboxAddress(_options.LeadRecipientName, _options.LeadRecipientEmail));
        message.ReplyTo.Add(new MailboxAddress(lead.Name, lead.Email));
        message.Subject = string.IsNullOrWhiteSpace(lead.Subject)
            ? "Novo lead do site Myrati"
            : $"Novo lead do site Myrati: {lead.Subject.Trim()}";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildHtmlBody(lead)
        };

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
        await smtpClient.AuthenticateAsync(
            new SaslMechanismOAuth2(_options.SenderEmail, accessToken),
            cancellationToken);
        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

        logger.LogInformation(
            "Lead publico {LeadId} enviado por e-mail para {RecipientEmail}",
            lead.LeadId,
            _options.LeadRecipientEmail);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(GmailContactLeadEmailSender));
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.GmailClientId,
            ["client_secret"] = _options.GmailClientSecret,
            ["refresh_token"] = _options.GmailRefreshToken,
            ["grant_type"] = "refresh_token"
        });

        using var response = await httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            content,
            cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(payload);
        if (!json.RootElement.TryGetProperty("access_token", out var accessTokenElement))
        {
            throw new InvalidOperationException("A resposta do Gmail OAuth2 nao retornou access_token.");
        }

        return accessTokenElement.GetString()
            ?? throw new InvalidOperationException("O access_token retornado pelo Gmail OAuth2 esta vazio.");
    }

    private static string BuildHtmlBody(ContactLeadEmailMessage lead)
    {
        var createdAt = lead.CreatedAt.UtcDateTime.ToString("dd/MM/yyyy HH:mm 'UTC'");
        var leadId = Encode(lead.LeadId);
        var name = Encode(lead.Name);
        var email = Encode(lead.Email);
        var company = Encode(string.IsNullOrWhiteSpace(lead.Company) ? "Nao informado" : lead.Company);
        var subject = Encode(string.IsNullOrWhiteSpace(lead.Subject) ? "Sem assunto" : lead.Subject);
        var message = Encode(lead.Message).Replace("\n", "<br />", StringComparison.Ordinal);

        return $$"""
<!doctype html>
<html lang="pt-BR">
  <body style="margin:0;padding:24px;background:#f4f1ea;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
    <div style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden;">
      <div style="padding:32px;background:linear-gradient(135deg,#0f766e,#134e4a);color:#ffffff;">
        <p style="margin:0 0 8px;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;opacity:0.8;">Myrati</p>
        <h1 style="margin:0;font-size:28px;line-height:1.2;">Novo lead de contato</h1>
      </div>
      <div style="padding:32px;">
        <p style="margin:0 0 16px;font-size:16px;line-height:1.6;">
          Um novo lead foi enviado pelo formulário público e precisa de acompanhamento comercial.
        </p>
        <table style="width:100%;border-collapse:collapse;margin:0 0 24px;">
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">Lead ID</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;">{{leadId}}</td>
          </tr>
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">Data</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;">{{createdAt}}</td>
          </tr>
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">Nome</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;">{{name}}</td>
          </tr>
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">E-mail</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;"><a href="mailto:{{email}}" style="color:#0f766e;">{{email}}</a></td>
          </tr>
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">Empresa</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;">{{company}}</td>
          </tr>
          <tr>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;font-weight:700;">Assunto</td>
            <td style="padding:10px 0;border-bottom:1px solid #e5e7eb;">{{subject}}</td>
          </tr>
        </table>
        <div style="padding:20px;border-radius:14px;background:#f8fafc;border:1px solid #e5e7eb;">
          <p style="margin:0 0 8px;font-size:14px;font-weight:700;text-transform:uppercase;letter-spacing:0.08em;color:#0f766e;">Mensagem</p>
          <p style="margin:0;font-size:16px;line-height:1.7;">{{message}}</p>
        </div>
      </div>
    </div>
  </body>
</html>
""";
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
