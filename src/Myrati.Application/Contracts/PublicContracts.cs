namespace Myrati.Application.Contracts;

public sealed record ContactRequest(string Name, string Email, string Company, string Subject, string Message);

public sealed record ContactResponse(string Message);

public sealed record LicenseActivationRequest(string ProductId, string LicenseKey);

public sealed record LicenseActivationResponse(
    string LicenseId,
    string ProductId,
    string ProductName,
    string ClientId,
    string ClientName,
    string Plan,
    string Status,
    string StartDate,
    string ExpiryDate,
    int MaxUsers,
    int ActiveUsers,
    string Message);

public sealed record PublicServiceStatusDto(string Id, string Name, string Status, string Uptime, string ResponseTime);

public sealed record PublicIncidentDto(string Id, string Date, string Title, string Description, bool Resolved);

public sealed record PublicUptimeSampleDto(string Id, string Day, decimal Pct);

public sealed record SystemStatusResponse(
    string OverallStatus,
    string LastUpdated,
    IReadOnlyCollection<PublicServiceStatusDto> Services,
    IReadOnlyCollection<PublicIncidentDto> Incidents,
    IReadOnlyCollection<PublicUptimeSampleDto> UptimeHistory);
