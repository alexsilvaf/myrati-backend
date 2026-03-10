namespace Myrati.Application.Contracts;

public sealed record ProductPlanDto(
    string Id,
    string Name,
    int MaxUsers,
    decimal MonthlyPrice,
    decimal? DevelopmentCost,
    decimal? MaintenanceCost,
    decimal? RevenueSharePercent);

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
    decimal MonthlyValue,
    decimal? DevelopmentCost,
    decimal? RevenueSharePercent);

public sealed record ProductSprintDto(
    string Id,
    string ProductId,
    string Name,
    string StartDate,
    string EndDate,
    string Status);

public sealed record ProductTaskDto(
    string Id,
    string ProductId,
    string SprintId,
    string Title,
    string Description,
    string Column,
    string Priority,
    string Assignee,
    IReadOnlyCollection<string> Tags,
    string CreatedDate);

public sealed record ProductKanbanDto(
    IReadOnlyCollection<ProductSprintDto> Sprints,
    IReadOnlyCollection<ProductTaskDto> Tasks,
    IReadOnlyCollection<string> AvailableAssignees);

public sealed record ProductPermissionSetDto(
    bool View,
    bool Create,
    bool Edit,
    bool Delete);

public sealed record ProductCollaboratorPermissionsDto(
    ProductPermissionSetDto Tasks,
    ProductPermissionSetDto Sprints,
    ProductPermissionSetDto Licenses,
    ProductPermissionSetDto Product);

public sealed record ProductCollaboratorSummaryDto(
    string MemberId,
    string MemberName,
    string MemberEmail,
    string MemberRole);

public sealed record ProductCollaboratorDto(
    string ProductId,
    string MemberId,
    string MemberName,
    string MemberEmail,
    string MemberRole,
    string AddedDate,
    ProductCollaboratorPermissionsDto Permissions);

public sealed record ProductSummaryDto(
    string Id,
    string Name,
    string Description,
    string Category,
    string Status,
    string SalesStrategy,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string CreatedDate,
    string Version,
    IReadOnlyCollection<ProductPlanDto> Plans,
    IReadOnlyCollection<ProductCollaboratorSummaryDto> Collaborators);

public sealed record ProductDetailDto(
    string Id,
    string Name,
    string Description,
    string Category,
    string Status,
    string SalesStrategy,
    int TotalLicenses,
    int ActiveLicenses,
    decimal MonthlyRevenue,
    string CreatedDate,
    string Version,
    IReadOnlyCollection<ProductPlanDto> Plans,
    IReadOnlyCollection<ProductCollaboratorDto> Collaborators,
    IReadOnlyCollection<LicenseDto> Licenses,
    ProductKanbanDto Kanban);

public sealed record UpsertProductPlanRequest(
    string Name,
    int MaxUsers,
    decimal MonthlyPrice,
    decimal? DevelopmentCost,
    decimal? MaintenanceCost,
    decimal? RevenueSharePercent);

public sealed record CreateProductRequest(
    string Name,
    string Description,
    string Category,
    string Status,
    string SalesStrategy,
    string Version,
    IReadOnlyCollection<UpsertProductPlanRequest> Plans);

public sealed record UpdateProductRequest(
    string Name,
    string Description,
    string Category,
    string Status,
    string SalesStrategy,
    string Version,
    IReadOnlyCollection<UpsertProductPlanRequest> Plans);

public sealed record CreateLicenseRequest(
    string ClientId,
    string Plan,
    decimal MonthlyValue,
    decimal? DevelopmentCost,
    decimal? RevenueSharePercent,
    string StartDate,
    string ExpiryDate);

public sealed record UpdateLicenseRequest(
    string ClientId,
    string Plan,
    decimal MonthlyValue,
    decimal? DevelopmentCost,
    decimal? RevenueSharePercent,
    string StartDate,
    string ExpiryDate);

public sealed record CreateProductSprintRequest(
    string Name,
    string StartDate,
    string EndDate,
    string Status);

public sealed record UpdateProductSprintRequest(
    string Name,
    string StartDate,
    string EndDate,
    string Status);

public sealed record CreateProductTaskRequest(
    string SprintId,
    string Title,
    string Description,
    string Column,
    string Priority,
    string Assignee,
    IReadOnlyCollection<string> Tags);

public sealed record UpdateProductTaskRequest(
    string SprintId,
    string Title,
    string Description,
    string Column,
    string Priority,
    string Assignee,
    IReadOnlyCollection<string> Tags);

public sealed record AddProductCollaboratorRequest(
    string MemberId,
    ProductCollaboratorPermissionsDto Permissions);

public sealed record UpdateProductCollaboratorRequest(
    ProductCollaboratorPermissionsDto Permissions);
