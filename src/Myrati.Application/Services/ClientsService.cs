using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Clients;
using Myrati.Domain.Identity;

namespace Myrati.Application.Services;

public sealed class ClientsService(
    IMyratiDbContext dbContext,
    IPasswordHasher passwordHasher,
    IPasswordSetupEmailSender passwordSetupEmailSender,
    IValidator<CreateClientRequest> createClientValidator,
    IValidator<UpdateClientRequest> updateClientValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : IClientsService
{
    private static readonly TimeSpan PasswordSetupTokenLifetime = TimeSpan.FromHours(72);

    public async Task<IReadOnlyCollection<ClientSummaryDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await dbContext.Clients
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var licenses = await dbContext.Licenses.ToListAsync(cancellationToken);
        var portalAccessUsers = await dbContext.AdminUsers
            .Where(x => x.Role == "Cliente")
            .ToListAsync(cancellationToken);
        var portalAccessByEmail = portalAccessUsers.ToDictionary(
            user => user.Email.ToLowerInvariant(),
            user => user);

        return clients
            .Select(client =>
            {
                portalAccessByEmail.TryGetValue(client.Email.Trim().ToLowerInvariant(), out var portalAccessUser);
                return MapSummary(client, licenses.Where(x => x.ClientId == client.Id), portalAccessUser);
            })
            .ToArray();
    }

    public async Task<ClientDetailDto> GetClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var licenses = await dbContext.Licenses
            .Where(x => x.ClientId == clientId)
            .OrderByDescending(x => x.Status == "Ativa")
            .ThenBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);
        var users = await dbContext.ConnectedUsers
            .Where(x => x.ClientId == clientId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var products = await dbContext.Products.ToListAsync(cancellationToken);
        var portalAccessUser = await FindClientPortalAccessUserByEmailAsync(client.Email, cancellationToken);

        return MapDetail(client, licenses, users, products, portalAccessUser);
    }

    public async Task<ClientDetailDto> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        await createClientValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureClientUniquenessAsync(request.Email, request.Document, null, null, cancellationToken);

        var clientId = IdGenerator.NextPrefixedId(
            "CLI-",
            await dbContext.Clients.Select(x => x.Id).ToListAsync(cancellationToken));

        var client = new Client
        {
            Id = clientId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Document = request.Document.Trim(),
            DocumentType = request.DocumentType,
            Company = request.Company.Trim(),
            Status = request.Status,
            JoinedDate = ApplicationTime.LocalToday()
        };

        var portalAccessUser = await CreateClientPortalAccessUserAsync(client, cancellationToken);
        var passwordSetup = await CreatePasswordSetupTokenAsync(portalAccessUser.Id, cancellationToken);
        await dbContext.AddAsync(client, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await passwordSetupEmailSender.SendAsync(
            portalAccessUser.Name,
            portalAccessUser.Email,
            passwordSetup.Token,
            passwordSetup.ExpiresAt,
            cancellationToken);
        var response = await GetClientAsync(clientId, cancellationToken);
        await PublishBackofficeEventAsync("client.created", response, cancellationToken);
        return response;
    }

    public async Task<ClientDetailDto> UpdateClientAsync(
        string clientId,
        UpdateClientRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateClientValidator.ValidateRequestAsync(request, cancellationToken);

        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var portalAccessUser = await FindClientPortalAccessUserByEmailAsync(client.Email, cancellationToken);
        await EnsureClientUniquenessAsync(request.Email, request.Document, clientId, portalAccessUser?.Id, cancellationToken);

        client.Name = request.Name.Trim();
        client.Email = request.Email.Trim();
        client.Phone = request.Phone.Trim();
        client.Document = request.Document.Trim();
        client.DocumentType = request.DocumentType;
        client.Company = request.Company.Trim();
        client.Status = request.Status;

        if (portalAccessUser is not null)
        {
            portalAccessUser.Name = request.Name.Trim();
            portalAccessUser.Email = request.Email.Trim();
            portalAccessUser.Phone = request.Phone.Trim();
            portalAccessUser.Department = request.Company.Trim();
            dbContext.Update(portalAccessUser);
        }

        dbContext.Update(client);
        await dbContext.SaveChangesAsync(cancellationToken);
        var response = await GetClientAsync(clientId, cancellationToken);
        await PublishBackofficeEventAsync("client.updated", response, cancellationToken);
        return response;
    }

    public async Task ResendPasswordSetupAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var portalAccessUser = await FindClientPortalAccessUserByEmailAsync(client.Email, cancellationToken);
        if (portalAccessUser is null)
        {
            portalAccessUser = await CreateClientPortalAccessUserAsync(client, cancellationToken);
        }
        else if (!string.Equals(portalAccessUser.Status, "Convite Pendente", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Este cliente já definiu a senha. O reenvio só fica disponível enquanto o convite estiver pendente.");
        }
        else
        {
            portalAccessUser.Name = client.Name;
            portalAccessUser.Email = client.Email;
            portalAccessUser.Phone = client.Phone;
            portalAccessUser.Department = client.Company;
            portalAccessUser.Role = "Cliente";
            portalAccessUser.Status = "Convite Pendente";
            dbContext.Update(portalAccessUser);
        }

        await RemovePasswordSetupTokensAsync(portalAccessUser.Id, cancellationToken);
        var passwordSetup = await CreatePasswordSetupTokenAsync(portalAccessUser.Id, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await passwordSetupEmailSender.SendAsync(
            portalAccessUser.Name,
            portalAccessUser.Email,
            passwordSetup.Token,
            passwordSetup.ExpiresAt,
            cancellationToken);

        await PublishBackofficeEventAsync(
            "client.password-setup-resent",
            new { client.Id, client.Name, client.Email },
            cancellationToken);
    }

    public async Task DeleteClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == clientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var licenses = await dbContext.Licenses
            .Where(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken);

        if (licenses.Any(x => x.Status == "Ativa"))
        {
            throw new ConflictException("Não é possível remover um cliente com licenças ativas.");
        }

        var users = await dbContext.ConnectedUsers
            .Where(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken);
        var portalAccessUser = await FindClientPortalAccessUserByEmailAsync(client.Email, cancellationToken);

        foreach (var user in users)
        {
            dbContext.Remove(user);
        }

        if (portalAccessUser is not null)
        {
            var sessions = await dbContext.ProfileSessions
                .Where(x => x.AdminUserId == portalAccessUser.Id)
                .ToListAsync(cancellationToken);
            var activities = await dbContext.ProfileActivities
                .Where(x => x.AdminUserId == portalAccessUser.Id)
                .ToListAsync(cancellationToken);
            var passwordSetupTokens = await dbContext.PasswordSetupTokens
                .Where(x => x.AdminUserId == portalAccessUser.Id)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                dbContext.Remove(session);
            }

            foreach (var activity in activities)
            {
                dbContext.Remove(activity);
            }

            foreach (var passwordSetupToken in passwordSetupTokens)
            {
                dbContext.Remove(passwordSetupToken);
            }

            dbContext.Remove(portalAccessUser);
        }

        foreach (var license in licenses)
        {
            dbContext.Remove(license);
        }

        dbContext.Remove(client);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "client.deleted",
            new { clientId = client.Id, client.Name, client.Company },
            cancellationToken);
    }

    private async Task EnsureClientUniquenessAsync(
        string email,
        string document,
        string? currentClientId,
        string? currentPortalAccessUserId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedDocument = document.Trim();

        var emailInUse = await dbContext.Clients.AnyAsync(
            x => x.Id != currentClientId && x.Email.ToLower() == normalizedEmail,
            cancellationToken);
        if (emailInUse)
        {
            throw new ConflictException($"Já existe um cliente com o e-mail '{email}'.");
        }

        var documentInUse = await dbContext.Clients.AnyAsync(
            x => x.Id != currentClientId && x.Document == normalizedDocument,
            cancellationToken);
        if (documentInUse)
        {
            throw new ConflictException($"Já existe um cliente com o documento '{document}'.");
        }

        var portalAccessEmailInUse = await dbContext.AdminUsers.AnyAsync(
            x => x.Id != currentPortalAccessUserId && x.Email.ToLower() == normalizedEmail,
            cancellationToken);
        if (portalAccessEmailInUse)
        {
            throw new ConflictException($"Já existe um usuário com o e-mail '{email}'.");
        }
    }

    private async Task<AdminUser> CreateClientPortalAccessUserAsync(
        Client client,
        CancellationToken cancellationToken)
    {
        var portalAccessUserId = IdGenerator.NextPrefixedId(
            "USR-",
            await dbContext.AdminUsers.Select(x => x.Id).ToListAsync(cancellationToken));

        var portalAccessUser = new AdminUser
        {
            Id = portalAccessUserId,
            Name = client.Name,
            Email = client.Email,
            Phone = client.Phone,
            Role = "Cliente",
            Status = "Convite Pendente",
            Department = client.Company,
            Location = string.Empty,
            PasswordHash = passwordHasher.Hash(IdGenerator.GenerateSecret(16)),
            IsPrimaryAccount = false
        };

        await dbContext.AddAsync(portalAccessUser, cancellationToken);
        return portalAccessUser;
    }

    private async Task<(string Token, DateTimeOffset ExpiresAt)> CreatePasswordSetupTokenAsync(
        string adminUserId,
        CancellationToken cancellationToken)
    {
        var passwordSetupTokenId = IdGenerator.NextPrefixedId(
            "PST-",
            await dbContext.PasswordSetupTokens.Select(x => x.Id).ToListAsync(cancellationToken));
        var passwordSetupToken = GeneratePasswordSetupToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(PasswordSetupTokenLifetime);

        await dbContext.AddAsync(new PasswordSetupToken
        {
            Id = passwordSetupTokenId,
            AdminUserId = adminUserId,
            TokenHash = ComputeTokenHash(passwordSetupToken),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt
        }, cancellationToken);

        return (passwordSetupToken, expiresAt);
    }

    private async Task RemovePasswordSetupTokensAsync(string adminUserId, CancellationToken cancellationToken)
    {
        var passwordSetupTokens = await dbContext.PasswordSetupTokens
            .Where(x => x.AdminUserId == adminUserId)
            .ToListAsync(cancellationToken);

        foreach (var passwordSetupToken in passwordSetupTokens)
        {
            dbContext.Remove(passwordSetupToken);
        }
    }

    private Task<AdminUser?> FindClientPortalAccessUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return dbContext.AdminUsers
            .FirstOrDefaultAsync(
                x => x.Role == "Cliente" && x.Email.ToLower() == normalizedEmail,
                cancellationToken);
    }

    private static string GeneratePasswordSetupToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim())));

    private static ClientSummaryDto MapSummary(
        Client client,
        IEnumerable<Domain.Products.License> licenses,
        AdminUser? portalAccessUser)
    {
        var licensesList = licenses.ToList();
        return new ClientSummaryDto(
            client.Id,
            client.Name,
            client.Email,
            client.Phone,
            client.Document,
            client.DocumentType,
            client.Company,
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            client.JoinedDate.ToIsoDate(),
            client.Status,
            string.Equals(portalAccessUser?.Status, "Convite Pendente", StringComparison.OrdinalIgnoreCase));
    }

    private static ClientDetailDto MapDetail(
        Client client,
        IEnumerable<Domain.Products.License> licenses,
        IEnumerable<ConnectedUser> users,
        IEnumerable<Domain.Products.Product> products,
        AdminUser? portalAccessUser)
    {
        var licensesList = licenses.ToList();
        var productsById = products.ToDictionary(x => x.Id);

        return new ClientDetailDto(
            client.Id,
            client.Name,
            client.Email,
            client.Phone,
            client.Document,
            client.DocumentType,
            client.Company,
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            client.JoinedDate.ToIsoDate(),
            client.Status,
            string.Equals(portalAccessUser?.Status, "Convite Pendente", StringComparison.OrdinalIgnoreCase),
            users.Select(user =>
            {
                var productName = productsById.TryGetValue(user.ProductId, out var product)
                    ? product.Name
                    : "Produto removido";
                return new UserDirectoryItemDto(
                    user.Id,
                    user.Name,
                    user.Email,
                    user.ClientId,
                    client.Company,
                    user.ProductId,
                    productName,
                    user.LastActiveDisplay,
                    user.Status);
            }).ToArray(),
            licensesList.Select(license =>
            {
                var productName = productsById.TryGetValue(license.ProductId, out var product)
                    ? product.Name
                    : "Produto removido";
                return new LicenseDto(
                    license.Id,
                    license.ClientId,
                    client.Company,
                    license.ProductId,
                    productName,
                    license.Plan,
                    license.MaxUsers,
                    license.ActiveUsers,
                    license.Status,
                    license.StartDate.ToIsoDate(),
                    license.ExpiryDate.ToIsoDate(),
                    license.MonthlyValue,
                    license.DevelopmentCost,
                    license.RevenueSharePercent);
            }).ToArray());
    }

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }
}
