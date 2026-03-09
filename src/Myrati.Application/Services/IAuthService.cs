using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default);
}
