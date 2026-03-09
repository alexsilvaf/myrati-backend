using Myrati.Domain.Identity;

namespace Myrati.Application.Abstractions;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(AdminUser user);
}
