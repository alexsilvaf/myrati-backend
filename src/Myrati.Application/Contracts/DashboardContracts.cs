namespace Myrati.Application.Contracts;

public sealed record MonthlyRevenueDto(string Month, decimal Revenue, int Licenses);

public sealed record RevenueByProductDto(string Name, decimal Value);

public sealed record RecentActivityDto(string Id, string Action, string Description, string Time, string Type);

public sealed record DashboardResponse(
    decimal TotalMonthlyRevenue,
    int ActiveLicensesCount,
    int TotalLicensesCount,
    int OnlineUsersCount,
    int TotalUsersCount,
    int UtilizationRate,
    int ActiveClients,
    IReadOnlyCollection<MonthlyRevenueDto> MonthlyRevenue,
    IReadOnlyCollection<RevenueByProductDto> RevenueByProduct,
    IReadOnlyCollection<RecentActivityDto> RecentActivity);
