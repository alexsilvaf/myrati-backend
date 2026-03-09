using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IProfileService
{
    Task<ProfileSnapshotDto> GetAsync(string email, CancellationToken cancellationToken = default);
    Task<ProfileInfoDto> UpdateAsync(string email, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(string email, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task RevokeSessionAsync(string email, string sessionId, CancellationToken cancellationToken = default);
}
