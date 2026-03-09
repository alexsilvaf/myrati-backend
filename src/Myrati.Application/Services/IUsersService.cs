using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IUsersService
{
    Task<IReadOnlyCollection<UserDirectoryItemDto>> GetUsersAsync(UserDirectoryQuery query, CancellationToken cancellationToken = default);
}
