using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IClientsService
{
    Task<IReadOnlyCollection<ClientSummaryDto>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<ClientDetailDto> GetClientAsync(string clientId, CancellationToken cancellationToken = default);
    Task<ClientDetailDto> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default);
    Task<ClientDetailDto> UpdateClientAsync(string clientId, UpdateClientRequest request, CancellationToken cancellationToken = default);
    Task DeleteClientAsync(string clientId, CancellationToken cancellationToken = default);
}
