using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Public;

namespace Myrati.Application.Services;

public sealed class PublicSiteService(
    IMyratiDbContext dbContext,
    FluentValidation.IValidator<ContactRequest> contactValidator,
    IRealtimeEventPublisher realtimeEventPublisher,
    IBackofficeNotificationPublisher backofficeNotificationPublisher) : IPublicSiteService
{
    public async Task<ContactResponse> SubmitContactAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default)
    {
        await contactValidator.ValidateRequestAsync(request, cancellationToken);

        var leadId = IdGenerator.NextPrefixedId(
            "LEAD-",
            await dbContext.ContactLeads.Select(x => x.Id).ToListAsync(cancellationToken));

        await dbContext.AddAsync(new ContactLead
        {
            Id = leadId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Company = request.Company.Trim(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        var payload = new
        {
            Id = leadId,
            request.Name,
            request.Email,
            request.Company,
            request.Subject
        };

        await realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, "contact.received", DateTimeOffset.UtcNow, payload),
            cancellationToken);
        await backofficeNotificationPublisher.PublishAsync("contact.received", payload, cancellationToken);

        return new ContactResponse("Mensagem enviada com sucesso.");
    }

    public async Task<SystemStatusResponse> GetSystemStatusAsync(CancellationToken cancellationToken = default)
    {
        var metadata = await dbContext.SystemStatusMetadata.FirstOrDefaultAsync(cancellationToken);
        var services = await dbContext.SystemComponentStatuses
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var incidents = await dbContext.SystemIncidents
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        var uptimeHistory = await dbContext.UptimeSamples
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var overallStatus = services.All(x => x.Status == "operational")
            ? "Todos os sistemas operacionais"
            : services.Any(x => x.Status == "outage")
                ? "Interrupcao detectada"
                : "Degradacao parcial detectada";

        return new SystemStatusResponse(
            overallStatus,
            metadata?.LastUpdatedDisplay ?? DateTime.Now.ToString("dd MMM yyyy 'as' HH:mm"),
            services.Select(x => new PublicServiceStatusDto(x.Id, x.Name, x.Status, x.Uptime, x.ResponseTime)).ToArray(),
            incidents.Select(x => new PublicIncidentDto(x.Id, x.DateDisplay, x.Title, x.Description, x.Resolved)).ToArray(),
            uptimeHistory.Select(x => new PublicUptimeSampleDto(x.Id, x.Day, x.Percentage)).ToArray());
    }
}
