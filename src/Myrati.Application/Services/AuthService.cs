using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Identity;

namespace Myrati.Application.Services;

public sealed class AuthService(
    IMyratiDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IValidator<LoginRequest> loginValidator,
    IValidator<PasswordSetupRequest> passwordSetupValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : IAuthService
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

    public async Task<PasswordSetupSessionDto> GetPasswordSetupSessionAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var passwordSetupToken = await GetValidPasswordSetupTokenAsync(token, cancellationToken);

        return new PasswordSetupSessionDto(
            passwordSetupToken.AdminUser?.Name ?? string.Empty,
            passwordSetupToken.AdminUser?.Email ?? string.Empty,
            passwordSetupToken.ExpiresAt);
    }

    public async Task CompletePasswordSetupAsync(
        PasswordSetupRequest request,
        CancellationToken cancellationToken = default)
    {
        await passwordSetupValidator.ValidateRequestAsync(request, cancellationToken);

        var passwordSetupToken = await GetValidPasswordSetupTokenAsync(request.Token, cancellationToken);
        var user = passwordSetupToken.AdminUser
            ?? throw new UnauthorizedAccessException("O link para definir senha e invalido ou expirou.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.Status = "Ativo";
        passwordSetupToken.UsedAt = DateTimeOffset.UtcNow;

        var staleTokens = await dbContext.PasswordSetupTokens
            .Where(x => x.AdminUserId == user.Id && x.Id != passwordSetupToken.Id)
            .ToListAsync(cancellationToken);

        foreach (var staleToken in staleTokens)
        {
            dbContext.Remove(staleToken);
        }

        dbContext.Update(user);
        dbContext.Update(passwordSetupToken);

        await dbContext.AddAsync(new ProfileActivity
        {
            Id = IdGenerator.NextPrefixedId(
                "ACT-",
                await dbContext.ProfileActivities.Select(x => x.Id).ToListAsync(cancellationToken)),
            AdminUserId = user.Id,
            Action = "Senha definida",
            DateDisplay = ApplicationTime.FormatLocalNow("dd/MM/yyyy HH:mm")
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        await PublishBackofficeEventAsync(
            "auth.password-setup-completed",
            new { user.Id, user.Name, user.Email, user.Role },
            cancellationToken);
    }

    private async Task<PasswordSetupToken> GetValidPasswordSetupTokenAsync(
        string token,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new UnauthorizedAccessException("O link para definir senha e invalido ou expirou.");
        }

        var tokenHash = ComputeTokenHash(token);
        var passwordSetupToken = await dbContext.PasswordSetupTokens
            .Include(x => x.AdminUser)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (passwordSetupToken is null ||
            passwordSetupToken.UsedAt is not null ||
            passwordSetupToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedAccessException("O link para definir senha e invalido ou expirou.");
        }

        return passwordSetupToken;
    }

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    private static AuthUserDto MapUser(Domain.Identity.AdminUser user) =>
        new(user.Id, user.Name, user.Email, user.Role);

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }
}
