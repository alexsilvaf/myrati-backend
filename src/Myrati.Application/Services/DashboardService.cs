using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public sealed class DashboardService(
    IMyratiDbContext dbContext,
    ICurrentUserContext currentUserContext) : IDashboardService
{
    private const string DeveloperRole = "Desenvolvedor";

    public Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default) =>
        string.Equals(currentUserContext.Role, DeveloperRole, StringComparison.Ordinal)
            ? GetDeveloperDashboardAsync(cancellationToken)
            : GetGlobalDashboardAsync(cancellationToken);

    private async Task<DashboardResponse> GetGlobalDashboardAsync(CancellationToken cancellationToken)
    {
        var licenses = await dbContext.Licenses.ToListAsync(cancellationToken);
        var clients = await dbContext.Clients.ToListAsync(cancellationToken);
        var products = await dbContext.Products.ToListAsync(cancellationToken);
        var plans = await dbContext.ProductPlans.ToListAsync(cancellationToken);
        var expenses = await dbContext.ProductExpenses.ToListAsync(cancellationToken);
        var cashTransactions = await dbContext.CashTransactions.ToListAsync(cancellationToken);
        var users = await dbContext.ConnectedUsers.ToListAsync(cancellationToken);
        var snapshots = await dbContext.RevenueSnapshots
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var activities = await dbContext.ActivityFeedItems
            .OrderBy(x => x.SortOrder)
            .Take(8)
            .ToListAsync(cancellationToken);
        var currentMonthStart = GetCurrentMonthStart();
        var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);

        var activeLicenses = licenses.Count(x => x.Status == "Ativa");
        var totalMaxUsers = licenses
            .Where(x => x.MaxUsers.HasValue)
            .Sum(x => x.MaxUsers ?? 0);
        var totalActiveUsers = licenses.Sum(x => x.ActiveUsers);

        var utilizationRate = totalMaxUsers == 0
            ? 0
            : (int)Math.Round((double)totalActiveUsers / totalMaxUsers * 100, MidpointRounding.AwayFromZero);
        var activeLicensesOnly = licenses.Where(x => x.Status == "Ativa").ToArray();
        var productsById = products.ToDictionary(x => x.Id);
        var clientsById = clients.ToDictionary(x => x.Id);
        var plansByProductId = plans.ToLookup(x => x.ProductId);
        var expensesByProductId = expenses.ToLookup(x => x.ProductId);
        var licensesByProductId = licenses.ToLookup(x => x.ProductId);
        var revenueByProductId = products.ToDictionary(
            product => product.Id,
            product => CalculateProductMonthlyRevenue(
                product,
                plansByProductId[product.Id].ToArray(),
                licensesByProductId[product.Id].ToArray(),
                expensesByProductId[product.Id].ToArray()),
            StringComparer.Ordinal);
        var currentMonthTransactionRevenueByProductId = BuildTransactionRevenueByProductId(
            cashTransactions,
            currentMonthStart,
            currentMonthEnd);
        var dashboardRevenueByProductId = BuildDashboardRevenueByProductId(
            products,
            revenueByProductId,
            currentMonthTransactionRevenueByProductId);
        var productExpensesById = products.ToDictionary(
            product => product.Id,
            product => CalculateProductMonthlyExpenses(expensesByProductId[product.Id].ToArray()),
            StringComparer.Ordinal);
        var totalMonthlyProductExpenses = productExpensesById.Values.Sum();
        var availableBalance = revenueByProductId.Values.Sum()
            - totalMonthlyProductExpenses
            + CalculateTransactionNetAmount(cashTransactions);

        var revenueByProduct = products
            .Select(product => new RevenueByProductDto(product.Name, dashboardRevenueByProductId[product.Id]))
            .Where(item => item.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToArray();

        var productHealth = products
            .Where(x => x.Status == "Ativo")
            .Select(product =>
            {
                var productLicenses = activeLicensesOnly
                    .Where(license => license.ProductId == product.Id)
                    .ToArray();
                var capacity = productLicenses
                    .Where(license => license.MaxUsers.HasValue)
                    .Sum(license => license.MaxUsers ?? 0);
                var used = productLicenses.Sum(license => license.ActiveUsers);
                var rate = capacity == 0
                    ? 0
                    : (int)Math.Round((double)used / capacity * 100, MidpointRounding.AwayFromZero);

                return new DashboardProductHealthDto(
                    product.Id,
                    product.Name,
                    capacity,
                    used,
                    dashboardRevenueByProductId[product.Id],
                    rate);
            })
            .OrderByDescending(x => x.Revenue)
            .ToArray();

        var topClients = activeLicensesOnly
            .Where(x => clientsById.TryGetValue(x.ClientId, out var client) && client.Status == "Ativo")
            .GroupBy(x => x.ClientId)
            .Select(group =>
            {
                var client = clientsById[group.Key];
                return new DashboardTopClientDto(client.Id, client.Company, group.Sum(x => x.MonthlyValue));
            })
            .OrderByDescending(x => x.MonthlyRevenue)
            .Take(5)
            .ToArray();

        var alerts = BuildAlerts(licenses, clientsById, productsById, productHealth, null);

        return new DashboardResponse(
            availableBalance,
            dashboardRevenueByProductId.Values.Sum()
                + CalculateUnassignedTransactionRevenueForMonth(cashTransactions, currentMonthStart, currentMonthEnd),
            totalMonthlyProductExpenses,
            activeLicenses,
            licenses.Count,
            users.Count(x => x.Status == "Online"),
            users.Count,
            utilizationRate,
            clients.Count(x => x.Status == "Ativo"),
            true,
            BuildGlobalMonthlyRevenue(snapshots, cashTransactions, currentMonthStart),
            revenueByProduct,
            activities.Select(x => new RecentActivityDto(x.Id, x.Action, x.Description, x.TimeDisplay, x.Type)).ToArray(),
            alerts,
            productHealth,
            topClients);
    }

    private async Task<DashboardResponse> GetDeveloperDashboardAsync(CancellationToken cancellationToken)
    {
        var currentUserId = GetRequiredCurrentUserId();
        var collaboratorAccess = await dbContext.ProductCollaborators
            .Where(x => x.MemberId == currentUserId)
            .ToListAsync(cancellationToken);

        if (collaboratorAccess.Count == 0)
        {
            return BuildEmptyDashboard();
        }

        var visibleProductIds = collaboratorAccess
            .Select(x => x.ProductId)
            .ToHashSet(StringComparer.Ordinal);
        var revenueVisibleProductIds = collaboratorAccess
            .Where(x => x.PlansView)
            .Select(x => x.ProductId)
            .ToHashSet(StringComparer.Ordinal);

        var products = await dbContext.Products
            .Where(x => visibleProductIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
        {
            return BuildEmptyDashboard();
        }

        var productIds = products.Select(x => x.Id).ToArray();
        var licenses = await dbContext.Licenses
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var users = await dbContext.ConnectedUsers
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var plans = await dbContext.ProductPlans
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var expenses = await dbContext.ProductExpenses
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);
        var cashTransactions = revenueVisibleProductIds.Count == 0
            ? []
            : await dbContext.CashTransactions
                .Where(x => !string.IsNullOrEmpty(x.ReferenceProductId) && revenueVisibleProductIds.Contains(x.ReferenceProductId))
                .ToListAsync(cancellationToken);
        var visibleClientIds = licenses
            .Select(x => x.ClientId)
            .Concat(users.Select(x => x.ClientId))
            .Distinct()
            .ToArray();
        var clients = visibleClientIds.Length == 0
            ? []
            : await dbContext.Clients
                .Where(x => visibleClientIds.Contains(x.Id))
                .ToListAsync(cancellationToken);

        var activeLicensesOnly = licenses.Where(x => x.Status == "Ativa").ToArray();
        var totalMaxUsers = licenses
            .Where(x => x.MaxUsers.HasValue)
            .Sum(x => x.MaxUsers ?? 0);
        var totalActiveUsers = licenses.Sum(x => x.ActiveUsers);
        var utilizationRate = totalMaxUsers == 0
            ? 0
            : (int)Math.Round((double)totalActiveUsers / totalMaxUsers * 100, MidpointRounding.AwayFromZero);
        var productsById = products.ToDictionary(x => x.Id);
        var clientsById = clients.ToDictionary(x => x.Id);
        var plansByProductId = plans.ToLookup(x => x.ProductId);
        var expensesByProductId = expenses.ToLookup(x => x.ProductId);
        var licensesByProductId = licenses.ToLookup(x => x.ProductId);
        var currentMonthStart = GetCurrentMonthStart();
        var currentMonthEnd = currentMonthStart.AddMonths(1).AddDays(-1);
        var revenueByProductId = products.ToDictionary(
            product => product.Id,
            product => revenueVisibleProductIds.Contains(product.Id)
                ? CalculateProductMonthlyRevenue(
                    product,
                    plansByProductId[product.Id].ToArray(),
                    licensesByProductId[product.Id].ToArray(),
                    expensesByProductId[product.Id].ToArray())
                : 0m,
            StringComparer.Ordinal);
        var currentMonthTransactionRevenueByProductId = BuildTransactionRevenueByProductId(
            cashTransactions,
            currentMonthStart,
            currentMonthEnd);
        var dashboardRevenueByProductId = BuildDashboardRevenueByProductId(
            products,
            revenueByProductId,
            currentMonthTransactionRevenueByProductId);
        var productExpensesById = products.ToDictionary(
            product => product.Id,
            product => revenueVisibleProductIds.Contains(product.Id)
                ? CalculateProductMonthlyExpenses(expensesByProductId[product.Id].ToArray())
                : 0m,
            StringComparer.Ordinal);
        var totalMonthlyProductExpenses = productExpensesById.Values.Sum();
        var availableBalance = revenueByProductId.Values.Sum()
            - totalMonthlyProductExpenses
            + CalculateTransactionNetAmount(cashTransactions);

        var productHealth = products
            .Select(product =>
            {
                var productLicenses = activeLicensesOnly
                    .Where(license => license.ProductId == product.Id)
                    .ToArray();
                var capacity = productLicenses
                    .Where(license => license.MaxUsers.HasValue)
                    .Sum(license => license.MaxUsers ?? 0);
                var used = productLicenses.Sum(license => license.ActiveUsers);
                var rate = capacity == 0
                    ? 0
                    : (int)Math.Round((double)used / capacity * 100, MidpointRounding.AwayFromZero);

                return new DashboardProductHealthDto(
                    product.Id,
                    product.Name,
                    capacity,
                    used,
                    dashboardRevenueByProductId[product.Id],
                    rate);
            })
            .OrderByDescending(x => x.Revenue)
            .ThenBy(x => x.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var revenueByProduct = products
            .Where(product => revenueVisibleProductIds.Contains(product.Id))
            .Select(product => new RevenueByProductDto(product.Name, dashboardRevenueByProductId[product.Id]))
            .Where(x => x.Value > 0)
            .OrderByDescending(x => x.Value)
            .ToArray();

        var topClients = activeLicensesOnly
            .Where(x => revenueVisibleProductIds.Contains(x.ProductId))
            .Where(x => clientsById.TryGetValue(x.ClientId, out var client) && client.Status == "Ativo")
            .GroupBy(x => x.ClientId)
            .Select(group =>
            {
                var client = clientsById[group.Key];
                return new DashboardTopClientDto(client.Id, client.Company, group.Sum(x => x.MonthlyValue));
            })
            .OrderByDescending(x => x.MonthlyRevenue)
            .Take(5)
            .ToArray();

        var alerts = BuildAlerts(licenses, clientsById, productsById, productHealth, revenueVisibleProductIds);

        return new DashboardResponse(
            availableBalance,
            dashboardRevenueByProductId.Values.Sum(),
            totalMonthlyProductExpenses,
            activeLicensesOnly.Count(),
            licenses.Count,
            users.Count(x => x.Status == "Online"),
            users.Count,
            utilizationRate,
            clients.Count(x => x.Status == "Ativo"),
            revenueVisibleProductIds.Count > 0,
            BuildDeveloperMonthlyRevenue(products, plansByProductId, expensesByProductId, licensesByProductId, revenueByProductId, revenueVisibleProductIds, cashTransactions),
            revenueByProduct,
            [],
            alerts,
            productHealth,
            topClients);
    }

    private static DashboardResponse BuildEmptyDashboard() =>
        new(
            0m,
            0m,
            0m,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            [],
            [],
            [],
            [],
            [],
            []);

    private static MonthlyRevenueDto[] BuildDeveloperMonthlyRevenue(
        IReadOnlyCollection<Domain.Products.Product> products,
        ILookup<string, Domain.Products.ProductPlan> plansByProductId,
        ILookup<string, Domain.Products.ProductExpense> expensesByProductId,
        ILookup<string, Domain.Products.License> licensesByProductId,
        IReadOnlyDictionary<string, decimal> currentRevenueByProductId,
        IReadOnlySet<string> revenueVisibleProductIds,
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions)
    {
        if (revenueVisibleProductIds.Count == 0)
        {
            return [];
        }

        var currentMonthStart = new DateOnly(ApplicationTime.LocalToday().Year, ApplicationTime.LocalToday().Month, 1);
        var firstMonthStart = currentMonthStart.AddMonths(-11);

        return Enumerable.Range(0, 12)
            .Select(index =>
            {
                var monthStart = firstMonthStart.AddMonths(index);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthRevenue = 0m;
                var licenseCount = 0;

                foreach (var product in products.Where(product => revenueVisibleProductIds.Contains(product.Id)))
                {
                    var activeLicenses = licensesByProductId[product.Id]
                        .Where(license => IsLicenseActiveInMonth(license, monthStart, monthEnd))
                        .ToArray();
                    var activeLicenseRevenue = activeLicenses.Sum(license => license.MonthlyValue);

                    monthRevenue += activeLicenseRevenue;
                    licenseCount += activeLicenses.Length;

                    if (monthStart == currentMonthStart && activeLicenseRevenue <= 0)
                    {
                        monthRevenue += currentRevenueByProductId[product.Id];
                    }
                }

                monthRevenue += CalculateTransactionRevenueForMonth(cashTransactions, monthStart, monthEnd);

                return new MonthlyRevenueDto(
                    FormatMonthLabel(monthStart),
                    monthRevenue,
                    licenseCount);
            })
            .ToArray();
    }

    private static MonthlyRevenueDto[] BuildGlobalMonthlyRevenue(
        IReadOnlyList<Domain.Dashboard.RevenueSnapshot> snapshots,
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions,
        DateOnly currentMonthStart)
    {
        if (snapshots.Count == 0)
        {
            return [];
        }

        return snapshots
            .Select((snapshot, index) =>
            {
                var monthStart = currentMonthStart.AddMonths(index - (snapshots.Count - 1));
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                return new MonthlyRevenueDto(
                    snapshot.Month,
                    snapshot.Revenue + CalculateTransactionRevenueForMonth(cashTransactions, monthStart, monthEnd),
                    snapshot.Licenses);
            })
            .ToArray();
    }

    private static bool IsLicenseActiveInMonth(Domain.Products.License license, DateOnly monthStart, DateOnly monthEnd)
    {
        if (string.Equals(license.Status, "Suspensa", StringComparison.Ordinal))
        {
            return false;
        }

        return license.StartDate <= monthEnd && license.ExpiryDate >= monthStart;
    }

    private static string FormatMonthLabel(DateOnly monthStart)
    {
        var monthLabel = CultureInfo
            .GetCultureInfo("pt-BR")
            .DateTimeFormat
            .GetAbbreviatedMonthName(monthStart.Month)
            .TrimEnd('.');

        return monthLabel.Length == 0
            ? monthStart.Month.ToString(CultureInfo.InvariantCulture)
            : char.ToUpperInvariant(monthLabel[0]) + monthLabel[1..];
    }

    private static DashboardAlertDto[] BuildAlerts(
        IReadOnlyCollection<Domain.Products.License> licenses,
        IReadOnlyDictionary<string, Domain.Clients.Client> clientsById,
        IReadOnlyDictionary<string, Domain.Products.Product> productsById,
        IReadOnlyCollection<DashboardProductHealthDto> productHealth,
        IReadOnlySet<string>? planVisibleProductIds)
    {
        var today = ApplicationTime.LocalToday();
        var alerts = new List<DashboardAlertDto>();

        foreach (var license in licenses.Where(x => x.Status == "Ativa"))
        {
            var daysToExpire = license.ExpiryDate.DayNumber - today.DayNumber;
            if (daysToExpire <= 0)
            {
                continue;
            }

            if (daysToExpire > 90)
            {
                continue;
            }

            var severity = daysToExpire <= 30 ? "critical" : "warning";
            var canViewPlan = planVisibleProductIds is null || planVisibleProductIds.Contains(license.ProductId);
            alerts.Add(new DashboardAlertDto(
                $"exp-{license.Id}",
                severity,
                $"Expira em {daysToExpire} dias",
                canViewPlan
                    ? $"{ResolveClientName(license.ClientId, clientsById)} — {ResolveProductName(license.ProductId, productsById)} ({license.Plan})"
                    : $"{ResolveClientName(license.ClientId, clientsById)} — {ResolveProductName(license.ProductId, productsById)}",
                "/admin/products"));
        }

        foreach (var license in licenses.Where(x => x.Status == "Suspensa"))
        {
            alerts.Add(new DashboardAlertDto(
                $"sus-{license.Id}",
                "critical",
                "Licença suspensa",
                $"{ResolveClientName(license.ClientId, clientsById)} — {ResolveProductName(license.ProductId, productsById)}",
                "/admin/products"));
        }

        foreach (var license in licenses.Where(x => x.Status == "Expirada"))
        {
            alerts.Add(new DashboardAlertDto(
                $"dead-{license.Id}",
                "warning",
                "Licença expirada",
                $"{ResolveClientName(license.ClientId, clientsById)} — {ResolveProductName(license.ProductId, productsById)}",
                "/admin/products"));
        }

        foreach (var product in productHealth.Where(x => x.Capacity > 0 && x.UtilizationRate >= 90))
        {
            alerts.Add(new DashboardAlertDto(
                $"cap-{product.ProductId}",
                "warning",
                $"Capacidade {product.UtilizationRate}%",
                $"{product.ProductName} — {product.Used}/{product.Capacity} usuários",
                $"/admin/products/{product.ProductId}"));
        }

        return alerts
            .OrderBy(x => GetSeverityOrder(x.Severity))
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int GetSeverityOrder(string severity) => severity switch
    {
        "critical" => 0,
        "warning" => 1,
        _ => 2
    };

    private static IReadOnlyDictionary<string, decimal> BuildDashboardRevenueByProductId(
        IReadOnlyCollection<Domain.Products.Product> products,
        IReadOnlyDictionary<string, decimal> revenueByProductId,
        IReadOnlyDictionary<string, decimal> transactionRevenueByProductId) =>
        products.ToDictionary(
            product => product.Id,
            product => revenueByProductId[product.Id]
                + transactionRevenueByProductId.GetValueOrDefault(product.Id),
            StringComparer.Ordinal);

    private static IReadOnlyDictionary<string, decimal> BuildTransactionRevenueByProductId(
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions,
        DateOnly monthStart,
        DateOnly monthEnd) =>
        cashTransactions
            .Where(transaction => string.Equals(transaction.Type, "deposit", StringComparison.Ordinal))
            .Where(transaction => !string.IsNullOrWhiteSpace(transaction.ReferenceProductId))
            .Where(transaction => transaction.Date >= monthStart && transaction.Date <= monthEnd)
            .GroupBy(transaction => transaction.ReferenceProductId!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(transaction => transaction.Amount),
                StringComparer.Ordinal);

    private static decimal CalculateTransactionRevenueForMonth(
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions,
        DateOnly monthStart,
        DateOnly monthEnd) =>
        cashTransactions
            .Where(transaction => string.Equals(transaction.Type, "deposit", StringComparison.Ordinal))
            .Where(transaction => transaction.Date >= monthStart && transaction.Date <= monthEnd)
            .Sum(transaction => transaction.Amount);

    private static decimal CalculateUnassignedTransactionRevenueForMonth(
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions,
        DateOnly monthStart,
        DateOnly monthEnd) =>
        cashTransactions
            .Where(transaction => string.Equals(transaction.Type, "deposit", StringComparison.Ordinal))
            .Where(transaction => string.IsNullOrWhiteSpace(transaction.ReferenceProductId))
            .Where(transaction => transaction.Date >= monthStart && transaction.Date <= monthEnd)
            .Sum(transaction => transaction.Amount);

    private static decimal CalculateTransactionNetAmount(
        IReadOnlyCollection<Domain.Costs.CashTransaction> cashTransactions) =>
        cashTransactions.Sum(transaction =>
            string.Equals(transaction.Type, "deposit", StringComparison.Ordinal)
                ? transaction.Amount
                : -transaction.Amount);

    private static DateOnly GetCurrentMonthStart()
    {
        var today = ApplicationTime.LocalToday();
        return new DateOnly(today.Year, today.Month, 1);
    }

    private static decimal CalculateProductMonthlyRevenue(
        Domain.Products.Product product,
        IReadOnlyCollection<Domain.Products.ProductPlan> plans,
        IReadOnlyCollection<Domain.Products.License> licenses,
        IReadOnlyCollection<Domain.Products.ProductExpense> expenses)
    {
        if (!string.Equals(product.Status, "Ativo", StringComparison.Ordinal))
        {
            return 0m;
        }

        var activeLicenseRevenue = licenses
            .Where(x => x.Status == "Ativa")
            .Sum(x => x.MonthlyValue);

        return product.SalesStrategy switch
        {
            "development" or "revenue_share" when activeLicenseRevenue <= 0 =>
                CalculateMaintenanceMonthlyRevenue(plans, expenses),
            _ => activeLicenseRevenue
        };
    }

    private static decimal CalculateMaintenanceMonthlyRevenue(
        IReadOnlyCollection<Domain.Products.ProductPlan> plans,
        IReadOnlyCollection<Domain.Products.ProductExpense> expenses)
    {
        var monthlyExpenses = expenses.Sum(CalculateMonthlyExpenseEquivalent);
        var fixedMaintenancePrice = plans
            .Where(plan => plan.MaintenanceCost.HasValue)
            .Select(plan => plan.MaintenanceCost!.Value)
            .DefaultIfEmpty(0m)
            .Average();

        var averageProfitMargin = plans
            .Where(plan => plan.MaintenanceProfitMargin.HasValue)
            .Select(plan => plan.MaintenanceProfitMargin!.Value)
            .DefaultIfEmpty(0m)
            .Average();

        var marginOnExpenses = monthlyExpenses * (averageProfitMargin / 100m);
        var maintenanceRevenue = fixedMaintenancePrice + marginOnExpenses;
        return Math.Round(maintenanceRevenue, 0, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateProductMonthlyExpenses(
        IReadOnlyCollection<Domain.Products.ProductExpense> expenses) =>
        expenses.Sum(CalculateMonthlyExpenseEquivalent);

    private static decimal CalculateMonthlyExpenseEquivalent(Domain.Products.ProductExpense expense) =>
        expense.Recurrence switch
        {
            "monthly" => expense.Amount,
            "annual" => expense.Amount / 12m,
            _ => 0m
        };

    private static string ResolveClientName(
        string clientId,
        IReadOnlyDictionary<string, Domain.Clients.Client> clientsById) =>
        clientsById.TryGetValue(clientId, out var client)
            ? client.Company
            : "Cliente removido";

    private static string ResolveProductName(
        string productId,
        IReadOnlyDictionary<string, Domain.Products.Product> productsById) =>
        productsById.TryGetValue(productId, out var product)
            ? product.Name
            : "Produto removido";

    private string GetRequiredCurrentUserId()
    {
        if (!currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(currentUserContext.UserId))
        {
            throw new ForbiddenException("Não foi possível identificar o usuário autenticado.");
        }

        return currentUserContext.UserId;
    }
}
