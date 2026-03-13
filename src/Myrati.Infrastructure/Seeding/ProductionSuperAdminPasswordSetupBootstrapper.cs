using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Domain.Identity;
using Myrati.Infrastructure.Persistence;

namespace Myrati.Infrastructure.Seeding;

public sealed class ProductionSuperAdminPasswordSetupBootstrapper(
    IPasswordHasher passwordHasher,
    IPasswordSetupEmailSender passwordSetupEmailSender)
{
    private const string BootstrapPassword = "Myrati@123";
    private static readonly TimeSpan PasswordSetupTokenLifetime = TimeSpan.FromHours(72);
    private static readonly string[] ProductionSuperAdminEmails =
    [
        "alex@myrati.com.br",
        "yasmin@myrati.com.br"
    ];

    public async Task SendInvitationsAsync(MyratiDbContext context, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var admins = await context.AdminUsersSet
            .Where(user => user.Role == "Super Admin")
            .Where(user => ProductionSuperAdminEmails.Contains(user.Email.ToLower()))
            .OrderBy(user => user.Email)
            .ToListAsync(cancellationToken);

        if (admins.Count == 0)
        {
            return;
        }

        var existingTokenIds = await context.PasswordSetupTokensSet
            .Select(token => token.Id)
            .ToListAsync(cancellationToken);
        var existingTokens = await context.PasswordSetupTokensSet
            .Select(token => new { token.AdminUserId, token.UsedAt, token.ExpiresAt })
            .ToListAsync(cancellationToken);
        var adminsWithActiveTokens = existingTokens
            .Where(token => token.UsedAt == null && token.ExpiresAt > now)
            .Select(token => token.AdminUserId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var pendingEmails = new List<(string Name, string Email, string Token, DateTimeOffset ExpiresAt)>();

        foreach (var admin in admins)
        {
            var needsPasswordSetup = string.Equals(admin.Status, "Convite Pendente", StringComparison.OrdinalIgnoreCase)
                || HasBootstrapPassword(admin);
            if (!needsPasswordSetup)
            {
                continue;
            }

            if (adminsWithActiveTokens.Contains(admin.Id, StringComparer.Ordinal))
            {
                continue;
            }

            if (HasBootstrapPassword(admin))
            {
                admin.PasswordHash = passwordHasher.Hash(IdGenerator.GenerateSecret(16));
            }

            admin.Status = "Convite Pendente";
            context.Update(admin);

            var rawToken = GeneratePasswordSetupToken();
            var expiresAt = now.Add(PasswordSetupTokenLifetime);
            var tokenId = IdGenerator.NextPrefixedId("PST-", existingTokenIds);
            existingTokenIds.Add(tokenId);

            await context.AddAsync(new PasswordSetupToken
            {
                Id = tokenId,
                AdminUserId = admin.Id,
                TokenHash = ComputeTokenHash(rawToken),
                CreatedAt = now,
                ExpiresAt = expiresAt
            }, cancellationToken);
            adminsWithActiveTokens.Add(admin.Id);

            pendingEmails.Add((admin.Name, admin.Email, rawToken, expiresAt));
        }

        if (pendingEmails.Count == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        foreach (var email in pendingEmails)
        {
            await passwordSetupEmailSender.SendAsync(
                email.Name,
                email.Email,
                email.Token,
                email.ExpiresAt,
                cancellationToken);
        }
    }

    private bool HasBootstrapPassword(AdminUser admin)
    {
        if (string.IsNullOrWhiteSpace(admin.PasswordHash))
        {
            return true;
        }

        try
        {
            return passwordHasher.Verify(BootstrapPassword, admin.PasswordHash);
        }
        catch
        {
            return false;
        }
    }

    private static string GeneratePasswordSetupToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));
}
