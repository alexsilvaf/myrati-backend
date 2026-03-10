using Myrati.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public sealed class DashboardService(IMyratiDbContext dbContext) : IDashboardService
{
    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var licenses = await dbContext.Licenses.ToListAsync(cancellationToken);
        var clients = await dbContext.Clients.ToListAsync(cancellationToken);
        var products = await dbContext.Products.ToListAsync(cancellationToken);
        var users = await dbContext.ConnectedUsers.ToListAsync(cancellationToken);
        var snapshots = await dbContext.RevenueSnapshots
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var activities = await dbContext.ActivityFeedItems
            .OrderBy(x => x.SortOrder)
            .Take(8)
            .ToListAsync(cancellationToken);

        var activeLicenses = licenses.Count(x => x.Status == "Ativa");
        var totalMaxUsers = licenses.Sum(x => x.MaxUsers);
        var totalActiveUsers = licenses.Sum(x => x.ActiveUsers);
        var totalRevenue = licenses
            .Where(x => x.Status == "Ativa")
            .Sum(x => x.MonthlyValue);

        var utilizationRate = totalMaxUsers == 0
            ? 0
            : (int)Math.Round((double)totalActiveUsers / totalMaxUsers * 100, MidpointRounding.AwayFromZero);
        var activeLicensesOnly = licenses.Where(x => x.Status == "Ativa").ToArray();
        var productsById = products.ToDictionary(x => x.Id);
        var clientsById = clients.ToDictionary(x => x.Id);

        var revenueByProduct = activeLicensesOnly
            .Where(x => productsById.ContainsKey(x.ProductId))
            .GroupBy(x => productsById[x.ProductId].Name)
            .Select(group => new RevenueByProductDto(group.Key, group.Sum(x => x.MonthlyValue)))
            .OrderByDescending(x => x.Value)
            .ToArray();

        var productHealth = products
            .Where(x => x.Status == "Ativo")
            .Select(product =>
            {
                var productLicenses = activeLicensesOnly
                    .Where(license => license.ProductId == product.Id)
                    .ToArray();
                var capacity = productLicenses.Sum(license => license.MaxUsers);
                var used = productLicenses.Sum(license => license.ActiveUsers);
                var revenue = productLicenses.Sum(license => license.MonthlyValue);
                var rate = capacity == 0
                    ? 0
                    : (int)Math.Round((double)used / capacity * 100, MidpointRounding.AwayFromZero);

                return new DashboardProductHealthDto(
                    product.Id,
                    product.Name,
                    capacity,
                    used,
                    revenue,
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

        var alerts = BuildAlerts(licenses, clientsById, productsById, productHealth);

        return new DashboardResponse(
            totalRevenue,
            activeLicenses,
            licenses.Count,
            users.Count(x => x.Status == "Online"),
            users.Count,
            utilizationRate,
            clients.Count(x => x.Status == "Ativo"),
            snapshots.Select(x => new MonthlyRevenueDto(x.Month, x.Revenue, x.Licenses)).ToArray(),
            revenueByProduct,
            activities.Select(x => new RecentActivityDto(x.Id, x.Action, x.Description, x.TimeDisplay, x.Type)).ToArray(),
            alerts,
            productHealth,
            topClients);
    }

    private static DashboardAlertDto[] BuildAlerts(
        IReadOnlyCollection<Domain.Products.License> licenses,
        IReadOnlyDictionary<string, Domain.Clients.Client> clientsById,
        IReadOnlyDictionary<string, Domain.Products.Product> productsById,
        IReadOnlyCollection<DashboardProductHealthDto> productHealth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
            alerts.Add(new DashboardAlertDto(
                $"exp-{license.Id}",
                severity,
                $"Expira em {daysToExpire} dias",
                $"{ResolveClientName(license.ClientId, clientsById)} — {ResolveProductName(license.ProductId, productsById)} ({license.Plan})",
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
}
