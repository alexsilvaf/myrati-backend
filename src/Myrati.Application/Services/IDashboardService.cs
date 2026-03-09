using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default);
}
