using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Costs;

namespace Myrati.Application.Services;

public sealed class TransactionsService(
    IMyratiDbContext dbContext,
    ICurrentUserContext currentUserContext,
    IValidator<CreateCashTransactionRequest> createTransactionValidator,
    IValidator<UpdateCashTransactionRequest> updateTransactionValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : ITransactionsService
{
    private const string DeveloperRole = "Desenvolvedor";

    public async Task<IReadOnlyCollection<CashTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var visibleProductIds = await GetVisibleProductIdsAsync(cancellationToken);
        if (visibleProductIds is { Count: 0 })
        {
            return [];
        }

        var transactions = await dbContext.CashTransactions
            .Where(transaction =>
                visibleProductIds == null
                || (!string.IsNullOrWhiteSpace(transaction.ReferenceProductId)
                    && visibleProductIds.Contains(transaction.ReferenceProductId)))
            .ToListAsync(cancellationToken);

        return MapTransactionsWithBalance(
            transactions
                .OrderBy(transaction => transaction.Date)
                .ThenBy(transaction => transaction.CreatedAtUtc.UtcTicks)
                .ThenBy(transaction => transaction.Id, StringComparer.Ordinal)
                .ToArray());
    }

    public async Task<CashTransactionDto> CreateTransactionAsync(
        CreateCashTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        await createTransactionValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await ResolveReferenceProductAsync(request.ReferenceProductId, cancellationToken);
        var transaction = new CashTransaction
        {
            Id = IdGenerator.NextPrefixedId(
                "TXN-",
                await dbContext.CashTransactions.Select(x => x.Id).ToListAsync(cancellationToken)),
            Type = request.Type,
            Category = request.Category,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            ReferenceProductId = product?.Id ?? string.Empty,
            ReferenceProductName = product?.Name ?? string.Empty,
            Date = RequestValidation.ParseIsoDate(request.Date, nameof(request.Date)),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        await dbContext.AddAsync(transaction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = (await GetTransactionsAsync(cancellationToken))
            .Single(item => item.Id == transaction.Id);
        await PublishBackofficeEventAsync("cash.transaction.created", response, cancellationToken);
        return response;
    }

    public async Task<CashTransactionDto> UpdateTransactionAsync(
        string transactionId,
        UpdateCashTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateTransactionValidator.ValidateRequestAsync(request, cancellationToken);

        var transaction = await GetTransactionEntityAsync(transactionId, cancellationToken);
        var product = await ResolveReferenceProductAsync(request.ReferenceProductId, cancellationToken);

        transaction.Type = request.Type;
        transaction.Category = request.Category;
        transaction.Amount = request.Amount;
        transaction.Description = request.Description.Trim();
        transaction.ReferenceProductId = product?.Id ?? string.Empty;
        transaction.ReferenceProductName = product?.Name ?? string.Empty;
        transaction.Date = RequestValidation.ParseIsoDate(request.Date, nameof(request.Date));

        dbContext.Update(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = (await GetTransactionsAsync(cancellationToken))
            .Single(item => item.Id == transaction.Id);
        await PublishBackofficeEventAsync("cash.transaction.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await GetTransactionEntityAsync(transactionId, cancellationToken);
        dbContext.Remove(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "cash.transaction.deleted",
            new { transaction.Id, transaction.Description, transaction.ReferenceProductId, transaction.ReferenceProductName },
            cancellationToken);
    }

    private async Task<HashSet<string>?> GetVisibleProductIdsAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(currentUserContext.Role, DeveloperRole, StringComparison.Ordinal))
        {
            return null;
        }

        var currentUserId = GetRequiredCurrentUserId();
        var visibleProductIds = await dbContext.ProductCollaborators
            .Where(collaborator => collaborator.MemberId == currentUserId && collaborator.PlansView)
            .Select(collaborator => collaborator.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return visibleProductIds.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<Domain.Products.Product?> ResolveReferenceProductAsync(string? productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            return null;
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellationToken);
        if (product is null)
        {
            throw new EntityNotFoundException("Produto", productId);
        }

        return product;
    }

    private async Task<CashTransaction> GetTransactionEntityAsync(string transactionId, CancellationToken cancellationToken) =>
        await dbContext.CashTransactions.FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken)
        ?? throw new EntityNotFoundException("Transação", transactionId);

    private static IReadOnlyCollection<CashTransactionDto> MapTransactionsWithBalance(IReadOnlyList<CashTransaction> transactions)
    {
        var balance = 0m;
        var result = new List<CashTransactionDto>(transactions.Count);
        foreach (var transaction in transactions)
        {
            balance += transaction.Type switch
            {
                "deposit" => transaction.Amount,
                _ => -transaction.Amount
            };

            result.Add(new CashTransactionDto(
                transaction.Id,
                transaction.Type,
                transaction.Category,
                transaction.Amount,
                transaction.Description,
                string.IsNullOrWhiteSpace(transaction.ReferenceProductId) ? null : transaction.ReferenceProductId,
                string.IsNullOrWhiteSpace(transaction.ReferenceProductName) ? null : transaction.ReferenceProductName,
                transaction.Date.ToIsoDate(),
                balance));
        }

        return result;
    }

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }

    private string GetRequiredCurrentUserId()
    {
        if (!currentUserContext.IsAuthenticated || string.IsNullOrWhiteSpace(currentUserContext.UserId))
        {
            throw new ForbiddenException("Não foi possível identificar o usuário autenticado.");
        }

        return currentUserContext.UserId;
    }
}
