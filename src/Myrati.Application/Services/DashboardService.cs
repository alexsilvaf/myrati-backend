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

        return new DashboardResponse(
            totalRevenue,
            activeLicenses,
            licenses.Count,
            users.Count(x => x.Status == "Online"),
            users.Count,
            utilizationRate,
            clients.Count(x => x.Status == "Ativo"),
            snapshots.Select(x => new MonthlyRevenueDto(x.Month, x.Revenue, x.Licenses)).ToArray(),
            licenses
                .Join(
                    dbContext.Products,
                    license => license.ProductId,
                    product => product.Id,
                    (license, product) => new { product.Name, license.MonthlyValue, license.Status })
                .AsEnumerable()
                .Where(x => x.Status == "Ativa")
                .GroupBy(x => x.Name)
                .Select(group => new RevenueByProductDto(group.Key, group.Sum(x => x.MonthlyValue)))
                .OrderByDescending(x => x.Value)
                .ToArray(),
            activities.Select(x => new RecentActivityDto(x.Id, x.Action, x.Description, x.TimeDisplay, x.Type)).ToArray());
    }
}
