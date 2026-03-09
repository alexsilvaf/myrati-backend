namespace Myrati.Application.Contracts;

public sealed record CompanyInfoDto(
    string Name,
    string Cnpj,
    string Email,
    string Phone,
    string Address,
    string City);

public sealed record RegionalPreferencesDto(string Language, string Timezone);

public sealed record NotificationPreferencesDto(
    bool EmailNotifications,
    bool PushNotifications,
    bool LicenseAlerts,
    bool UsageAlerts,
    bool WeeklyReport);

public sealed record SecurityPreferencesDto(bool TwoFactorAuth, string SessionTimeout);

public sealed record ApiKeyDto(string Id, string Label, string Prefix, string Key, bool Active, string CreatedAt);

public sealed record TeamMemberDto(string Id, string Name, string Email, string Role, string Status);

public sealed record SettingsSnapshotDto(
    CompanyInfoDto CompanyInfo,
    RegionalPreferencesDto Regional,
    NotificationPreferencesDto Notifications,
    SecurityPreferencesDto Security,
    IReadOnlyCollection<ApiKeyDto> ApiKeys,
    IReadOnlyCollection<TeamMemberDto> TeamMembers);

public sealed record UpdateSettingsRequest(
    CompanyInfoDto CompanyInfo,
    RegionalPreferencesDto Regional,
    NotificationPreferencesDto Notifications,
    SecurityPreferencesDto Security);

public sealed record CreateApiKeyRequest(string Label, string Environment);

public sealed record CreateTeamMemberRequest(string Name, string Email, string Role);

public sealed record UpdateTeamMemberRequest(string Name, string Email, string Role, string Status);
