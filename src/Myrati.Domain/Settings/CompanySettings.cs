using Myrati.Domain.Common;

namespace Myrati.Domain.Settings;

public sealed class CompanySettings : Entity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Language { get; set; } = "pt-BR";
    public string Timezone { get; set; } = "America/Sao_Paulo";
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public bool LicenseAlerts { get; set; } = true;
    public bool UsageAlerts { get; set; } = true;
    public bool WeeklyReport { get; set; }
    public bool TwoFactorAuth { get; set; }
    public string SessionTimeout { get; set; } = "30";
}
