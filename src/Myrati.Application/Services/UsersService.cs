using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public sealed class UsersService(IMyratiDbContext dbContext) : IUsersService
{
    public async Task<IReadOnlyCollection<UserDirectoryItemDto>> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var users = await dbContext.ConnectedUsers
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var clients = await dbContext.Clients.ToListAsync(cancellationToken);
        var products = await dbContext.Products.ToListAsync(cancellationToken);

        var clientsById = clients.ToDictionary(x => x.Id);
        var productsById = products.ToDictionary(x => x.Id);

        var items = users.Select(user =>
        {
            var clientName = clientsById.TryGetValue(user.ClientId, out var client)
                ? client.Company
                : "Cliente removido";
            var productName = productsById.TryGetValue(user.ProductId, out var product)
                ? product.Name
                : "Produto removido";

            return new UserDirectoryItemDto(
                user.Id,
                user.Name,
                user.Email,
                user.ClientId,
                clientName,
                user.ProductId,
                productName,
                user.LastActiveDisplay,
                user.Status);
        });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            items = items.Where(x =>
                x.Name.ToLowerInvariant().Contains(search) ||
                x.Email.ToLowerInvariant().Contains(search) ||
                x.ClientName.ToLowerInvariant().Contains(search) ||
                x.ProductName.ToLowerInvariant().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items.Where(x => x.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.ProductId))
        {
            items = items.Where(x => x.ProductId == query.ProductId);
        }

        return items.ToArray();
    }
}
