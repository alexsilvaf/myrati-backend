using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Identity;

namespace Myrati.Application.Services;

public sealed class ProfileService(
    IMyratiDbContext dbContext,
    IPasswordHasher passwordHasher,
    IValidator<UpdateProfileRequest> updateProfileValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : IProfileService
{
    public async Task<ProfileSnapshotDto> GetAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(email, cancellationToken);
        var sessions = await dbContext.ProfileSessions
            .Where(x => x.AdminUserId == user.Id)
            .OrderByDescending(x => x.IsCurrent)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var activities = await dbContext.ProfileActivities
            .Where(x => x.AdminUserId == user.Id)
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return new ProfileSnapshotDto(
            new ProfileInfoDto(
                user.Id,
                user.Name,
                user.Email,
                user.Phone,
                user.Role,
                user.Department,
                user.Location),
            sessions.Select(x => new ProfileSessionDto(x.Id, x.Location, x.LastActiveDisplay, x.IsCurrent)).ToArray(),
            activities.Select(x => new ProfileActivityDto(x.Action, x.DateDisplay)).ToArray());
    }

    public async Task<ProfileInfoDto> UpdateAsync(
        string email,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateProfileValidator.ValidateRequestAsync(request, cancellationToken);

        var user = await GetUserByEmailAsync(email, cancellationToken);
        await EnsureAdminEmailAvailableAsync(request.Email, user.Id, cancellationToken);

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim();
        user.Phone = request.Phone.Trim();
        user.Department = request.Department.Trim();
        user.Location = request.Location.Trim();

        dbContext.Update(user);
        await AddActivityAsync(user.Id, "Perfil atualizado", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ProfileInfoDto(
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Role,
            user.Department,
            user.Location);
        await PublishBackofficeEventAsync("profile.updated", response, cancellationToken);
        return response;
    }

    public async Task ChangePasswordAsync(
        string email,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await changePasswordValidator.ValidateRequestAsync(request, cancellationToken);

        var user = await GetUserByEmailAsync(email, cancellationToken);
        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("A senha atual informada é inválida.");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        dbContext.Update(user);
        await AddActivityAsync(user.Id, "Senha alterada", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "profile.password-changed",
            new { user.Id, user.Email },
            cancellationToken);
    }

    public async Task RevokeSessionAsync(
        string email,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByEmailAsync(email, cancellationToken);
        var session = await dbContext.ProfileSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.AdminUserId == user.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Sessao", sessionId);

        if (session.IsCurrent)
        {
            throw new ConflictException("Nao e possivel encerrar a sessao atual.");
        }

        dbContext.Remove(session);
        await AddActivityAsync(user.Id, "Sessao revogada", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "profile.session-revoked",
            new { sessionId = session.Id, user.Id, user.Email },
            cancellationToken);
    }

    private async Task<AdminUser> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken)
            ?? throw new EntityNotFoundException("Usuario", email);
    }

    private async Task EnsureAdminEmailAvailableAsync(
        string email,
        string currentAdminId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var emailInUse = await dbContext.AdminUsers.AnyAsync(
            x => x.Id != currentAdminId && x.Email.ToLower() == normalizedEmail,
            cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException($"Ja existe um usuario com o e-mail '{email}'.");
        }
    }

    private async Task AddActivityAsync(string adminUserId, string action, CancellationToken cancellationToken)
    {
        var activityId = IdGenerator.NextPrefixedId(
            "ACT-",
            await dbContext.ProfileActivities.Select(x => x.Id).ToListAsync(cancellationToken));

        await dbContext.AddAsync(new ProfileActivity
        {
            Id = activityId,
            AdminUserId = adminUserId,
            Action = action,
            DateDisplay = ApplicationTime.FormatLocalNow("dd/MM/yyyy HH:mm")
        }, cancellationToken);
    }

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }
}
