namespace Myrati.Application.Contracts;

public sealed record MonthlyRevenueDto(string Month, decimal Revenue, int Licenses);

public sealed record RevenueByProductDto(string Name, decimal Value);

public sealed record RecentActivityDto(string Id, string Action, string Description, string Time, string Type);

public sealed record DashboardAlertDto(string Id, string Severity, string Label, string Detail, string Link);

public sealed record DashboardProductHealthDto(
    string ProductId,
    string ProductName,
    int Capacity,
    int Used,
    decimal Revenue,
    int UtilizationRate);

public sealed record DashboardTopClientDto(string ClientId, string Company, decimal MonthlyRevenue);

public sealed record DashboardResponse(
    decimal TotalMonthlyRevenue,
    int ActiveLicensesCount,
    int TotalLicensesCount,
    int OnlineUsersCount,
    int TotalUsersCount,
    int UtilizationRate,
    int ActiveClients,
    bool CanViewRevenue,
    IReadOnlyCollection<MonthlyRevenueDto> MonthlyRevenue,
    IReadOnlyCollection<RevenueByProductDto> RevenueByProduct,
    IReadOnlyCollection<RecentActivityDto> RecentActivity,
    IReadOnlyCollection<DashboardAlertDto> Alerts,
    IReadOnlyCollection<DashboardProductHealthDto> ProductHealth,
    IReadOnlyCollection<DashboardTopClientDto> TopClients);
