namespace Myrati.Application.Contracts;

public sealed record ProductPlanDto(string Id, string Name, int MaxUsers, decimal MonthlyPrice);

public sealed record LicenseDto(
    string Id,
    string ClientId,
    string ClientName,
    string ProductId,
    string ProductName,
    string Plan,
    int MaxUsers,
    int ActiveUsers,
    string Status,
    string StartDate,
    string ExpiryDate,
    decimal MonthlyValue);

public sealed record ProductSummaryDto(
    string Id,
    string Name,
    string Description,
    string Category,
    string Status,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string CreatedDate,
    string Version,
    IReadOnlyCollection<ProductPlanDto> Plans);

public sealed record ProductDetailDto(
    string Id,
    string Name,
    string Description,
    string Category,
    string Status,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string CreatedDate,
    string Version,
    IReadOnlyCollection<ProductPlanDto> Plans,
    IReadOnlyCollection<LicenseDto> Licenses);

public sealed record UpsertProductPlanRequest(string Name, int MaxUsers, decimal MonthlyPrice);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    string Category,
    string Status,
    string Version,
    IReadOnlyCollection<UpsertProductPlanRequest> Plans);

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    string Category,
    string Status,
    string Version,
    IReadOnlyCollection<UpsertProductPlanRequest> Plans);

public sealed record CreateLicenseRequest(
    string ClientId,
    string Plan,
    decimal MonthlyValue,
    string StartDate,
    string ExpiryDate);

public sealed record UpdateLicenseRequest(
    string ClientId,
    string Plan,
    decimal MonthlyValue,
    string StartDate,
    string ExpiryDate);
