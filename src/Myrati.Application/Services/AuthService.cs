using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;

namespace Myrati.Application.Services;

public sealed class AuthService(
    IMyratiDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IValidator<LoginRequest> loginValidator,
    IRealtimeEventPublisher realtimeEventPublisher) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateRequestAsync(request, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash) || user.Status != "Ativo")
        {
            throw new UnauthorizedAccessException("Credenciais inválidas.");
        }

        var token = jwtTokenService.GenerateAccessToken(user);
        var response = new AuthResponse(token.Token, token.ExpiresAt, MapUser(user));

        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(
                RealtimeChannels.Backoffice,
                "auth.login",
                DateTimeOffset.UtcNow,
                new { response.User.Id, response.User.Name, response.User.Email, response.User.Role }),
            cancellationToken);

        return response;
    }

    public async Task<AuthUserDto> GetCurrentUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken)
            ?? throw new EntityNotFoundException("Usuário", email);

        return MapUser(user);
    }

    private static AuthUserDto MapUser(Domain.Identity.AdminUser user) =>
        new(user.Id, user.Name, user.Email, user.Role);
}
