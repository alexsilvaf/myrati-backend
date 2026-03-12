using Microsoft.Extensions.Configuration;

namespace Myrati.Infrastructure.Email;

internal sealed class PasswordSetupEmailOptions
{
    public string FrontendBaseUrl { get; init; } = "http://localhost:4173";
    public string PasswordSetupPath { get; init; } = "/login";
    public string SenderName { get; init; } = "Myrati";
    public string SenderEmail { get; init; } = string.Empty;
    public string GmailClientId { get; init; } = string.Empty;
    public string GmailClientSecret { get; init; } = string.Empty;
    public string GmailRefreshToken { get; init; } = string.Empty;

    public bool HasGmailConfiguration =>
        !string.IsNullOrWhiteSpace(SenderEmail) &&
        !string.IsNullOrWhiteSpace(GmailClientId) &&
        !string.IsNullOrWhiteSpace(GmailClientSecret) &&
        !string.IsNullOrWhiteSpace(GmailRefreshToken);

    public string BuildPasswordSetupUrl(string token)
    {
        var baseUrl = FrontendBaseUrl.TrimEnd('/');
        var path = PasswordSetupPath.StartsWith("/", StringComparison.Ordinal)
            ? PasswordSetupPath
            : $"/{PasswordSetupPath}";

        return $"{baseUrl}{path}?setupToken={Uri.EscapeDataString(token)}";
    }

    public static PasswordSetupEmailOptions FromConfiguration(IConfiguration configuration) =>
        new()
        {
            FrontendBaseUrl = configuration["Email:FrontendBaseUrl"] ?? "http://localhost:4173",
            PasswordSetupPath = configuration["Email:PasswordSetupPath"] ?? "/login",
            SenderName = configuration["Email:SenderName"] ?? "Myrati",
            SenderEmail = configuration["Email:SenderEmail"] ?? string.Empty,
            GmailClientId = configuration["Email:Gmail:ClientId"] ?? string.Empty,
            GmailClientSecret = configuration["Email:Gmail:ClientSecret"] ?? string.Empty,
            GmailRefreshToken = configuration["Email:Gmail:RefreshToken"] ?? string.Empty
        };
}
