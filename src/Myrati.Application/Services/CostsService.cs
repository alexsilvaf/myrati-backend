using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Costs;

namespace Myrati.Application.Services;

public sealed class CostsService(
    IMyratiDbContext dbContext,
    IValidator<CreateCompanyCostRequest> createCostValidator,
    IValidator<UpdateCompanyCostRequest> updateCostValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : ICostsService
{
    public async Task<IReadOnlyCollection<CompanyCostDto>> GetCostsAsync(CancellationToken cancellationToken = default)
    {
        var costs = await dbContext.CompanyCosts
            .OrderBy(x => x.Status != "Ativo")
            .ThenByDescending(x => x.StartDate)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return costs.Select(MapCost).ToArray();
    }

    public async Task<CompanyCostDto> CreateCostAsync(
        CreateCompanyCostRequest request,
        CancellationToken cancellationToken = default)
    {
        await createCostValidator.ValidateRequestAsync(request, cancellationToken);

        var cost = new CompanyCost
        {
            Id = IdGenerator.NextPrefixedId(
                "CC-",
                await dbContext.CompanyCosts.Select(x => x.Id).ToListAsync(cancellationToken)),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category,
            Amount = request.Amount,
            Recurrence = request.Recurrence,
            Vendor = request.Vendor.Trim(),
            StartDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate)),
            NextBillingDate = ParseOptionalIsoDate(request.NextBillingDate, nameof(request.NextBillingDate)),
            Status = request.Status
        };

        await dbContext.AddAsync(cost, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapCost(cost);
        await PublishBackofficeEventAsync("company.cost.created", response, cancellationToken);
        return response;
    }

    public async Task<CompanyCostDto> UpdateCostAsync(
        string costId,
        UpdateCompanyCostRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateCostValidator.ValidateRequestAsync(request, cancellationToken);

        var cost = await GetCostEntityAsync(costId, cancellationToken);
        cost.Name = request.Name.Trim();
        cost.Description = request.Description.Trim();
        cost.Category = request.Category;
        cost.Amount = request.Amount;
        cost.Recurrence = request.Recurrence;
        cost.Vendor = request.Vendor.Trim();
        cost.StartDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        cost.NextBillingDate = ParseOptionalIsoDate(request.NextBillingDate, nameof(request.NextBillingDate));
        cost.Status = request.Status;

        dbContext.Update(cost);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapCost(cost);
        await PublishBackofficeEventAsync("company.cost.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteCostAsync(string costId, CancellationToken cancellationToken = default)
    {
        var cost = await GetCostEntityAsync(costId, cancellationToken);
        dbContext.Remove(cost);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync("company.cost.deleted", new { cost.Id, cost.Name }, cancellationToken);
    }

    private async Task<CompanyCost> GetCostEntityAsync(string costId, CancellationToken cancellationToken) =>
        await dbContext.CompanyCosts.FirstOrDefaultAsync(x => x.Id == costId, cancellationToken)
        ?? throw new EntityNotFoundException("Custo", costId);

    private static DateOnly? ParseOptionalIsoDate(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return RequestValidation.ParseIsoDate(value, fieldName);
    }

    private static CompanyCostDto MapCost(CompanyCost cost) =>
        new(
            cost.Id,
            cost.Name,
            cost.Description,
            cost.Category,
            cost.Amount,
            cost.Recurrence,
            cost.Vendor,
            cost.StartDate.ToIsoDate(),
            cost.NextBillingDate?.ToIsoDate(),
            cost.Status);

    private async ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken)
    {
        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync(eventType, payload, cancellationToken);
    }
}
