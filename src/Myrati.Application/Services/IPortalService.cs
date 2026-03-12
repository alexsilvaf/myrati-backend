using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IPortalService
{
    Task<PortalMeDto> GetPortalMeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserDirectoryItemDto>> GetLicenseUsersAsync(
        string licenseId,
        CancellationToken cancellationToken = default);
}
