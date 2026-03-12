using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthUserDto> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default);
    Task<PasswordSetupSessionDto> GetPasswordSetupSessionAsync(string token, CancellationToken cancellationToken = default);
    Task CompletePasswordSetupAsync(PasswordSetupRequest request, CancellationToken cancellationToken = default);
}
