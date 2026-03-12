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
using Myrati.Domain.Settings;

namespace Myrati.Application.Services;

public sealed class SettingsService(
    IMyratiDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPasswordSetupEmailSender passwordSetupEmailSender,
    IValidator<UpdateSettingsRequest> updateSettingsValidator,
    IValidator<CreateApiKeyRequest> createApiKeyValidator,
    IValidator<CreateTeamMemberRequest> createTeamMemberValidator,
    IValidator<UpdateTeamMemberRequest> updateTeamMemberValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : ISettingsService
{
    private static readonly TimeSpan PasswordSetupTokenLifetime = TimeSpan.FromHours(72);

    public async Task<SettingsSnapshotDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        var apiKeys = await dbContext.ApiKeys
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        var teamMembers = await dbContext.AdminUsers
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return MapSnapshot(settings, apiKeys, teamMembers);
    }

    public async Task<SettingsSnapshotDto> UpdateAsync(
        UpdateSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateSettingsValidator.ValidateRequestAsync(request, cancellationToken);

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.CompanyName = request.CompanyInfo.Name.Trim();
        settings.Cnpj = request.CompanyInfo.Cnpj.Trim();
        settings.ContactEmail = request.CompanyInfo.Email.Trim();
        settings.ContactPhone = request.CompanyInfo.Phone.Trim();
        settings.Address = request.CompanyInfo.Address.Trim();
        settings.City = request.CompanyInfo.City.Trim();
        settings.Language = request.Regional.Language.Trim();
        settings.Timezone = request.Regional.Timezone.Trim();
        settings.EmailNotifications = request.Notifications.EmailNotifications;
        settings.PushNotifications = request.Notifications.PushNotifications;
        settings.LicenseAlerts = request.Notifications.LicenseAlerts;
        settings.UsageAlerts = request.Notifications.UsageAlerts;
        settings.WeeklyReport = request.Notifications.WeeklyReport;
        settings.TwoFactorAuth = request.Security.TwoFactorAuth;
        settings.SessionTimeout = request.Security.SessionTimeout.Trim();

        dbContext.Update(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        var response = await GetAsync(cancellationToken);
        await PublishBackofficeEventAsync("settings.updated", response, cancellationToken);
        return response;
    }

    public async Task<ApiKeyDto> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken cancellationToken = default)
    {
        await createApiKeyValidator.ValidateRequestAsync(request, cancellationToken);

        var apiKeyId = IdGenerator.NextPrefixedId(
            "AK-",
            await dbContext.ApiKeys.Select(x => x.Id).ToListAsync(cancellationToken));
        var prefix = request.Environment == "production"
            ? $"myra_prod_{IdGenerator.GenerateSecret(4)}"
            : $"myra_stg_{IdGenerator.GenerateSecret(4)}";
        var secret = IdGenerator.GenerateSecret(24);

        var apiKey = new ApiKeyCredential
        {
            Id = apiKeyId,
            Label = request.Label.Trim(),
            Prefix = prefix,
            Secret = secret,
            Active = true,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.AddAsync(apiKey, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        var response = MapApiKey(apiKey, revealSecret: true);
        await PublishBackofficeEventAsync("apikey.created", response, cancellationToken);
        return response;
    }

    public async Task<ApiKeyDto> RotateApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await dbContext.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, cancellationToken)
            ?? throw new EntityNotFoundException("ApiKey", apiKeyId);

        apiKey.Secret = IdGenerator.GenerateSecret(24);
        dbContext.Update(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapApiKey(apiKey, revealSecret: true);
        await PublishBackofficeEventAsync("apikey.rotated", response, cancellationToken);
        return response;
    }

    public async Task<ApiKeyDto> ToggleApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await dbContext.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, cancellationToken)
            ?? throw new EntityNotFoundException("ApiKey", apiKeyId);

        apiKey.Active = !apiKey.Active;
        dbContext.Update(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapApiKey(apiKey, revealSecret: false);
        await PublishBackofficeEventAsync("apikey.toggled", response, cancellationToken);
        return response;
    }

    public async Task DeleteApiKeyAsync(string apiKeyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await dbContext.ApiKeys
            .FirstOrDefaultAsync(x => x.Id == apiKeyId, cancellationToken)
            ?? throw new EntityNotFoundException("ApiKey", apiKeyId);

        dbContext.Remove(apiKey);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "apikey.deleted",
            new { apiKeyId = apiKey.Id, apiKey.Label },
            cancellationToken);
    }

    public async Task<TeamMemberDto> CreateTeamMemberAsync(
        CreateTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        await createTeamMemberValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureAdminEmailAvailableAsync(request.Email, null, cancellationToken);

        var teamMemberId = IdGenerator.NextPrefixedId(
            "TM-",
            await dbContext.AdminUsers.Select(x => x.Id).ToListAsync(cancellationToken));

        var teamMember = new AdminUser
        {
            Id = teamMemberId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = string.Empty,
            Role = request.Role,
            Status = "Convite Pendente",
            Department = string.Empty,
            Location = string.Empty,
            PasswordHash = passwordHasher.Hash(IdGenerator.GenerateSecret(16)),
            IsPrimaryAccount = false
        };
        var passwordSetupTokenId = IdGenerator.NextPrefixedId(
            "PST-",
            await dbContext.PasswordSetupTokens.Select(x => x.Id).ToListAsync(cancellationToken));
        var passwordSetupToken = GeneratePasswordSetupToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(PasswordSetupTokenLifetime);

        await dbContext.AddAsync(teamMember, cancellationToken);
        await dbContext.AddAsync(new PasswordSetupToken
        {
            Id = passwordSetupTokenId,
            AdminUserId = teamMemberId,
            TokenHash = ComputeTokenHash(passwordSetupToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await passwordSetupEmailSender.SendAsync(
            teamMember.Name,
            teamMember.Email,
            passwordSetupToken,
            expiresAt,
            cancellationToken);

        var response = MapTeamMember(teamMember);
        await PublishBackofficeEventAsync("team-member.created", response, cancellationToken);
        return response;
    }

    public async Task<TeamMemberDto> UpdateTeamMemberAsync(
        string teamMemberId,
        UpdateTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateTeamMemberValidator.ValidateRequestAsync(request, cancellationToken);

        var teamMember = await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == teamMemberId, cancellationToken)
            ?? throw new EntityNotFoundException("Membro", teamMemberId);

        await EnsureAdminEmailAvailableAsync(request.Email, teamMemberId, cancellationToken);

        teamMember.Name = request.Name.Trim();
        teamMember.Email = request.Email.Trim();
        teamMember.Role = request.Role;
        teamMember.Status = request.Status;

        dbContext.Update(teamMember);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapTeamMember(teamMember);
        await PublishBackofficeEventAsync("team-member.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteTeamMemberAsync(string teamMemberId, CancellationToken cancellationToken = default)
    {
        var teamMember = await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Id == teamMemberId, cancellationToken)
            ?? throw new EntityNotFoundException("Membro", teamMemberId);

        if (teamMember.IsPrimaryAccount || teamMember.Role == "Super Admin")
        {
            throw new ConflictException("Não é possível remover o Super Admin.");
        }

        var sessions = await dbContext.ProfileSessions
            .Where(x => x.AdminUserId == teamMemberId)
            .ToListAsync(cancellationToken);
        var activities = await dbContext.ProfileActivities
            .Where(x => x.AdminUserId == teamMemberId)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            dbContext.Remove(session);
        }

        foreach (var activity in activities)
        {
            dbContext.Remove(activity);
        }

        var passwordSetupTokens = await dbContext.PasswordSetupTokens
            .Where(x => x.AdminUserId == teamMemberId)
            .ToListAsync(cancellationToken);

        foreach (var passwordSetupToken in passwordSetupTokens)
        {
            dbContext.Remove(passwordSetupToken);
        }

        dbContext.Remove(teamMember);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "team-member.deleted",
            new { teamMemberId = teamMember.Id, teamMember.Name, teamMember.Email },
            cancellationToken);
    }

    private async Task<CompanySettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new CompanySettings
        {
            Id = "CFG-001",
            CompanyName = "Myrati",
            Cnpj = string.Empty,
            ContactEmail = string.Empty,
            ContactPhone = string.Empty,
            Address = string.Empty,
            City = string.Empty,
            Language = "pt-BR",
            Timezone = "America/Sao_Paulo",
            EmailNotifications = true,
            PushNotifications = true,
            LicenseAlerts = true,
            UsageAlerts = true,
            WeeklyReport = false,
            TwoFactorAuth = false,
            SessionTimeout = "30"
        };

        await dbContext.AddAsync(settings, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private async Task EnsureAdminEmailAvailableAsync(
        string email,
        string? currentAdminId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var emailInUse = await dbContext.AdminUsers.AnyAsync(
            x => x.Id != currentAdminId && x.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException($"Ja existe um membro com o e-mail '{email}'.");
        }
    }

    private static SettingsSnapshotDto MapSnapshot(
        CompanySettings settings,
        IEnumerable<ApiKeyCredential> apiKeys,
        IEnumerable<AdminUser> teamMembers) =>
        new(
            new CompanyInfoDto(
                settings.CompanyName,
                settings.Cnpj,
                settings.ContactEmail,
                settings.ContactPhone,
                settings.Address,
                settings.City),
            new RegionalPreferencesDto(settings.Language, settings.Timezone),
            new NotificationPreferencesDto(
                settings.EmailNotifications,
                settings.PushNotifications,
                settings.LicenseAlerts,
                settings.UsageAlerts,
                settings.WeeklyReport),
            new SecurityPreferencesDto(settings.TwoFactorAuth, settings.SessionTimeout),
            apiKeys.Select(x => MapApiKey(x, revealSecret: false)).ToArray(),
            teamMembers.Select(MapTeamMember).ToArray());

    private static ApiKeyDto MapApiKey(ApiKeyCredential apiKey, bool revealSecret)
    {
        var visibleKey = revealSecret
            ? $"{apiKey.Prefix}{apiKey.Secret}"
            : $"{apiKey.Prefix}****************";

        return new ApiKeyDto(
            apiKey.Id,
            apiKey.Label,
            apiKey.Prefix,
            visibleKey,
            apiKey.Active,
            apiKey.CreatedAt.ToIsoDate());
    }

    private static TeamMemberDto MapTeamMember(AdminUser teamMember) =>
        new(teamMember.Id, teamMember.Name, teamMember.Email, teamMember.Role, teamMember.Status);

    private static string GeneratePasswordSetupToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }
}
