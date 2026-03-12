using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Clients;

namespace Myrati.Application.Services;

public sealed class ClientsService(
    IMyratiDbContext dbContext,
    IValidator<CreateClientRequest> createClientValidator,
    IValidator<UpdateClientRequest> updateClientValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : IClientsService
{
    public async Task<IReadOnlyCollection<ClientSummaryDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await dbContext.Clients
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var licenses = await dbContext.Licenses.ToListAsync(cancellationToken);

        return clients
            .Select(client => MapSummary(client, licenses.Where(x => x.ClientId == client.Id)))
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

        return MapDetail(client, licenses, users, products);
    }

    public async Task<ClientDetailDto> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        await createClientValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureClientUniquenessAsync(request.Email, request.Document, null, cancellationToken);

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
            JoinedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.AddAsync(client, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
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

        await EnsureClientUniquenessAsync(request.Email, request.Document, clientId, cancellationToken);

        client.Name = request.Name.Trim();
        client.Email = request.Email.Trim();
        client.Phone = request.Phone.Trim();
        client.Document = request.Document.Trim();
        client.DocumentType = request.DocumentType;
        client.Company = request.Company.Trim();
        client.Status = request.Status;

        dbContext.Update(client);
        await dbContext.SaveChangesAsync(cancellationToken);
        var response = await GetClientAsync(clientId, cancellationToken);
        await PublishBackofficeEventAsync("client.updated", response, cancellationToken);
        return response;
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

        foreach (var user in users)
        {
            dbContext.Remove(user);
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
    }

    private static ClientSummaryDto MapSummary(Client client, IEnumerable<Domain.Products.License> licenses)
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
            client.Status);
    }

    private static ClientDetailDto MapDetail(
        Client client,
        IEnumerable<Domain.Products.License> licenses,
        IEnumerable<ConnectedUser> users,
        IEnumerable<Domain.Products.Product> products)
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
