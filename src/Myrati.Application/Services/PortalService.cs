using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Domain.Clients;

namespace Myrati.Application.Services;

public sealed class PortalService(
    IMyratiDbContext dbContext,
    ICurrentUserContext currentUserContext) : IPortalService
{
    public async Task<PortalMeDto> GetPortalMeAsync(CancellationToken cancellationToken = default)
    {
        var client = await ResolveCurrentClientAsync(cancellationToken);
        var licenses = await dbContext.Licenses
            .Where(x => x.ClientId == client.Id)
            .OrderByDescending(x => x.Status == "Ativa")
            .ThenBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);
        var productIds = licenses
            .Select(license => license.ProductId)
            .Distinct()
            .ToArray();
        var products = await dbContext.Products
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        return new PortalMeDto(
            client.Id,
            client.Name,
            client.Email,
            client.Company,
            licenses
                .Select(license => MapLicense(license, client.Company, products))
                .ToArray());
    }

    public async Task<IReadOnlyCollection<UserDirectoryItemDto>> GetLicenseUsersAsync(
        string licenseId,
        CancellationToken cancellationToken = default)
    {
        var client = await ResolveCurrentClientAsync(cancellationToken);
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId && x.ClientId == client.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);

        var productName = await dbContext.Products
            .Where(x => x.Id == license.ProductId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Produto removido";

        var users = await dbContext.ConnectedUsers
            .Where(x => x.ClientId == client.Id && x.ProductId == license.ProductId)
            .OrderByDescending(x => x.Status == "Online")
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return users
            .Select(user => MapUser(user, client.Company, productName))
            .ToArray();
    }

    private async Task<Client> ResolveCurrentClientAsync(CancellationToken cancellationToken)
    {
        var email = currentUserContext.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Usuário autenticado sem e-mail para o portal.");
        }

        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (client is not null)
        {
            return client;
        }

        var linkedClientIds = await dbContext.ConnectedUsers
            .Where(x => x.Email.ToLower() == email)
            .Select(x => x.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return linkedClientIds.Count switch
        {
            0 => throw new ForbiddenException("Usuário autenticado não está vinculado a um cliente do portal."),
            > 1 => throw new ConflictException("Este usuário está vinculado a mais de um cliente. Entre em contato com o suporte."),
            _ => await dbContext.Clients
                .FirstOrDefaultAsync(x => x.Id == linkedClientIds[0], cancellationToken)
                ?? throw new EntityNotFoundException("Cliente", linkedClientIds[0])
        };
    }

    private static LicenseDto MapLicense(
        Domain.Products.License license,
        string clientCompany,
        IReadOnlyDictionary<string, Domain.Products.Product> productsById)
    {
        var productName = productsById.TryGetValue(license.ProductId, out var product)
            ? product.Name
            : "Produto removido";

        return new LicenseDto(
            license.Id,
            license.ClientId,
            clientCompany,
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
    }

    private static UserDirectoryItemDto MapUser(
        ConnectedUser user,
        string clientCompany,
        string productName) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.ClientId,
            clientCompany,
            user.ProductId,
            productName,
            user.LastActiveDisplay,
            user.Status);
}
