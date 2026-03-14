using System.Net.Http;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using SaslMechanismOAuth2 = MailKit.Security.SaslMechanismOAuth2;

namespace Myrati.Infrastructure.Email;

internal sealed class GmailPasswordSetupEmailSender(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<GmailPasswordSetupEmailSender> logger) : IPasswordSetupEmailSender
{
    private readonly PasswordSetupEmailOptions _options = PasswordSetupEmailOptions.FromConfiguration(configuration);

    public async Task SendAsync(
        string recipientName,
        string recipientEmail,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        var setupUrl = _options.BuildPasswordSetupUrl(token);
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderEmail));
        message.To.Add(new MailboxAddress(recipientName, recipientEmail));
        message.Subject = "Defina sua senha na Myrati";
        message.Body = new TextPart(TextFormat.Html)
        {
            Text = BuildHtmlBody(recipientName, setupUrl, expiresAt)
        };

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, cancellationToken);
        await smtpClient.AuthenticateAsync(
            new SaslMechanismOAuth2(_options.SenderEmail, accessToken),
            cancellationToken);
        await smtpClient.SendAsync(message, cancellationToken);
        await smtpClient.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Convite de definicao de senha enviado para {RecipientEmail}", recipientEmail);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(GmailPasswordSetupEmailSender));
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

    private static string BuildHtmlBody(string recipientName, string setupUrl, DateTimeOffset expiresAt)
    {
        var expirationText = $"{ApplicationTime.FormatLocal(expiresAt, "dd/MM/yyyy HH:mm")} (horário de Brasília)";

        return $$"""
<!doctype html>
<html lang="pt-BR">
  <body style="margin:0;padding:24px;background:#f4f1ea;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
    <div style="max-width:560px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:18px;overflow:hidden;">
      <div style="padding:32px;background:linear-gradient(135deg,#0f766e,#134e4a);color:#ffffff;">
        <p style="margin:0 0 8px;font-size:12px;letter-spacing:0.12em;text-transform:uppercase;opacity:0.8;">Myrati</p>
        <h1 style="margin:0;font-size:28px;line-height:1.2;">Defina sua senha</h1>
      </div>
      <div style="padding:32px;">
        <p style="margin:0 0 16px;font-size:16px;line-height:1.6;">Ola, {{recipientName}}.</p>
        <p style="margin:0 0 16px;font-size:16px;line-height:1.6;">
          Sua conta administrativa foi criada na Myrati. Para concluir o acesso, defina sua senha pelo link abaixo.
        </p>
        <p style="margin:24px 0;">
          <a href="{{setupUrl}}" style="display:inline-block;padding:14px 22px;background:#0f766e;color:#ffffff;text-decoration:none;border-radius:999px;font-weight:700;">
            Definir senha
          </a>
        </p>
        <p style="margin:0 0 12px;font-size:14px;line-height:1.6;">Este link expira em {{expirationText}}.</p>
        <p style="margin:0;font-size:14px;line-height:1.6;">
          Se o botao nao abrir, copie e cole este endereco no navegador:<br />
          <a href="{{setupUrl}}" style="color:#0f766e;">{{setupUrl}}</a>
        </p>
      </div>
    </div>
  </body>
</html>
""";
    }
}
