using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface ICostsService
{
    Task<IReadOnlyCollection<CompanyCostDto>> GetCostsAsync(CancellationToken cancellationToken = default);
    Task<CompanyCostDto> CreateCostAsync(CreateCompanyCostRequest request, CancellationToken cancellationToken = default);
    Task<CompanyCostDto> UpdateCostAsync(string costId, UpdateCompanyCostRequest request, CancellationToken cancellationToken = default);
    Task DeleteCostAsync(string costId, CancellationToken cancellationToken = default);
}
