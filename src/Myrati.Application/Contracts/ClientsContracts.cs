namespace Myrati.Application.Contracts;

public sealed record ClientSummaryDto(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Document,
    string DocumentType,
    string Company,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string JoinedDate,
    string Status);

public sealed record ClientDetailDto(
    string Id,
    string Name,
    string Email,
    string Phone,
    string Document,
    string DocumentType,
    string Company,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string JoinedDate,
    string Status,
    IReadOnlyCollection<UserDirectoryItemDto> Users,
    IReadOnlyCollection<LicenseDto> Licenses);

public sealed record CreateClientRequest(
    string Name,
    string Email,
    string Phone,
    string Document,
    string DocumentType,
    string Company,
    string Status);

public sealed record UpdateClientRequest(
    string Name,
    string Email,
    string Phone,
    string Document,
    string DocumentType,
    string Company,
    string Status);
