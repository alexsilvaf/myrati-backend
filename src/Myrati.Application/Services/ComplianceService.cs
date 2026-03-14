using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Validation;
using Myrati.Domain.Compliance;

namespace Myrati.Application.Services;

public sealed class ComplianceService(
    IMyratiDbContext dbContext,
    IConfiguration configuration,
    IValidator<CreateDataSubjectRequestRequest> createDataSubjectRequestValidator,
    IValidator<UpdateDataSubjectRequestRequest> updateDataSubjectRequestValidator,
    IValidator<CreateProcessingActivityRequest> createProcessingActivityValidator,
    IValidator<UpdateProcessingActivityRequest> updateProcessingActivityValidator,
    IValidator<CreateSecurityIncidentRequest> createSecurityIncidentValidator,
    IValidator<UpdateSecurityIncidentRequest> updateSecurityIncidentValidator) : IComplianceService
{
    private static readonly HashSet<string> ClosedSubjectRequestStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Concluida",
        "Negada",
        "Arquivada"
    };

    private static readonly HashSet<string> ActiveIncidentStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Aberto",
        "Em investigacao",
        "Contido",
        "Comunicado"
    };

    public async Task<ComplianceSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var subjectRequests = await dbContext.DataSubjectRequests
            .ToListAsync(cancellationToken);
        var processingActivities = await dbContext.ProcessingActivityRecords
            .OrderBy(x => x.Status)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var securityIncidents = await dbContext.SecurityIncidentRecords
            .ToListAsync(cancellationToken);
        var adminNames = await GetAdminNamesAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        return new ComplianceSnapshotDto(
            new ComplianceMetricsDto(
                subjectRequests.Count(x => !ClosedSubjectRequestStatuses.Contains(x.Status)),
                subjectRequests.Count(x =>
                    !ClosedSubjectRequestStatuses.Contains(x.Status)
                    && x.DueAtUtc <= now.AddDays(3)),
                securityIncidents.Count(x => ActiveIncidentStatuses.Contains(x.Status)),
                processingActivities.Count(x => x.ReviewDueAtUtc is not null && x.ReviewDueAtUtc <= now.AddDays(30))),
            subjectRequests
                .OrderByDescending(x => x.RequestedAtUtc)
                .Select(x => MapDataSubjectRequest(x, adminNames))
                .ToArray(),
            processingActivities.Select(MapProcessingActivity).ToArray(),
            securityIncidents
                .OrderByDescending(x => x.DetectedAtUtc)
                .Select(x => MapSecurityIncident(x, adminNames))
                .ToArray());
    }

    public async Task<DataSubjectRequestDto> CreateDataSubjectRequestAsync(
        CreateDataSubjectRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await createDataSubjectRequestValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureAssignedAdminExistsAsync(request.AssignedAdminUserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new DataSubjectRequest
        {
            Id = IdGenerator.NextPrefixedId(
                "DSR-",
                await dbContext.DataSubjectRequests.Select(x => x.Id).ToListAsync(cancellationToken)),
            SubjectName = request.SubjectName.Trim(),
            SubjectEmail = request.SubjectEmail.Trim(),
            SubjectDocument = request.SubjectDocument.Trim(),
            RequestType = request.RequestType,
            Channel = request.Channel,
            Status = "Recebida",
            Details = request.Details.Trim(),
            IdentityVerified = request.IdentityVerified,
            AssignedAdminUserId = NormalizeOptional(request.AssignedAdminUserId),
            RequestedAtUtc = now,
            DueAtUtc = request.DueAtUtc ?? now.AddDays(GetDefaultSubjectRequestDueDays()),
            UpdatedAtUtc = now
        };

        await dbContext.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var adminNames = await GetAdminNamesAsync(cancellationToken);
        return MapDataSubjectRequest(entity, adminNames);
    }

    public async Task<DataSubjectRequestDto> UpdateDataSubjectRequestAsync(
        string requestId,
        UpdateDataSubjectRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateDataSubjectRequestValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureAssignedAdminExistsAsync(request.AssignedAdminUserId, cancellationToken);

        var entity = await dbContext.DataSubjectRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken)
            ?? throw new EntityNotFoundException("Solicitacao do titular", requestId);

        var now = DateTimeOffset.UtcNow;
        entity.Status = request.Status;
        entity.Details = request.Details.Trim();
        entity.ResolutionSummary = NormalizeOptional(request.ResolutionSummary);
        entity.IdentityVerified = request.IdentityVerified;
        entity.AssignedAdminUserId = NormalizeOptional(request.AssignedAdminUserId);
        entity.DueAtUtc = request.DueAtUtc;
        entity.AcknowledgedAtUtc = request.AcknowledgedAtUtc
            ?? entity.AcknowledgedAtUtc
            ?? (string.Equals(request.Status, "Recebida", StringComparison.OrdinalIgnoreCase) ? null : now);
        entity.CompletedAtUtc = ClosedSubjectRequestStatuses.Contains(request.Status)
            ? request.CompletedAtUtc ?? entity.CompletedAtUtc ?? now
            : request.CompletedAtUtc;
        entity.UpdatedAtUtc = now;

        dbContext.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var adminNames = await GetAdminNamesAsync(cancellationToken);
        return MapDataSubjectRequest(entity, adminNames);
    }

    public async Task<ProcessingActivityDto> CreateProcessingActivityAsync(
        CreateProcessingActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        await createProcessingActivityValidator.ValidateRequestAsync(request, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new ProcessingActivityRecord
        {
            Id = IdGenerator.NextPrefixedId(
                "ROPA-",
                await dbContext.ProcessingActivityRecords.Select(x => x.Id).ToListAsync(cancellationToken)),
            Name = request.Name.Trim(),
            SystemName = request.SystemName.Trim(),
            Purpose = request.Purpose.Trim(),
            LegalBasis = request.LegalBasis,
            DataSubjectCategories = request.DataSubjectCategories.Trim(),
            PersonalDataCategories = request.PersonalDataCategories.Trim(),
            SharedWith = request.SharedWith.Trim(),
            RetentionPolicy = request.RetentionPolicy.Trim(),
            SecurityMeasures = request.SecurityMeasures.Trim(),
            OwnerArea = request.OwnerArea.Trim(),
            InternationalTransfer = request.InternationalTransfer,
            Status = request.Status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ReviewDueAtUtc = request.ReviewDueAtUtc
        };

        await dbContext.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapProcessingActivity(entity);
    }

    public async Task<ProcessingActivityDto> UpdateProcessingActivityAsync(
        string activityId,
        UpdateProcessingActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateProcessingActivityValidator.ValidateRequestAsync(request, cancellationToken);

        var entity = await dbContext.ProcessingActivityRecords
            .FirstOrDefaultAsync(x => x.Id == activityId, cancellationToken)
            ?? throw new EntityNotFoundException("Registro de tratamento", activityId);

        entity.Name = request.Name.Trim();
        entity.SystemName = request.SystemName.Trim();
        entity.Purpose = request.Purpose.Trim();
        entity.LegalBasis = request.LegalBasis;
        entity.DataSubjectCategories = request.DataSubjectCategories.Trim();
        entity.PersonalDataCategories = request.PersonalDataCategories.Trim();
        entity.SharedWith = request.SharedWith.Trim();
        entity.RetentionPolicy = request.RetentionPolicy.Trim();
        entity.SecurityMeasures = request.SecurityMeasures.Trim();
        entity.OwnerArea = request.OwnerArea.Trim();
        entity.InternationalTransfer = request.InternationalTransfer;
        entity.Status = request.Status;
        entity.ReviewDueAtUtc = request.ReviewDueAtUtc;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapProcessingActivity(entity);
    }

    public async Task<SecurityIncidentDto> CreateSecurityIncidentAsync(
        CreateSecurityIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        await createSecurityIncidentValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureAssignedAdminExistsAsync(request.AssignedAdminUserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var entity = new SecurityIncidentRecord
        {
            Id = IdGenerator.NextPrefixedId(
                "CIS-",
                await dbContext.SecurityIncidentRecords.Select(x => x.Id).ToListAsync(cancellationToken)),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Severity = request.Severity,
            Status = request.Status,
            ContainsPersonalData = request.ContainsPersonalData,
            AffectedDataSummary = request.AffectedDataSummary.Trim(),
            ImpactSummary = request.ImpactSummary.Trim(),
            MitigationSummary = request.MitigationSummary.Trim(),
            NotifyAnpd = request.NotifyAnpd,
            NotifyDataSubjects = request.NotifyDataSubjects,
            AssignedAdminUserId = NormalizeOptional(request.AssignedAdminUserId),
            DetectedAtUtc = request.DetectedAtUtc,
            OccurredAtUtc = request.OccurredAtUtc,
            ContainedAtUtc = request.ContainedAtUtc,
            ReportedToAnpdAtUtc = request.ReportedToAnpdAtUtc,
            ReportedToDataSubjectsAtUtc = request.ReportedToDataSubjectsAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var adminNames = await GetAdminNamesAsync(cancellationToken);
        return MapSecurityIncident(entity, adminNames);
    }

    public async Task<SecurityIncidentDto> UpdateSecurityIncidentAsync(
        string incidentId,
        UpdateSecurityIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateSecurityIncidentValidator.ValidateRequestAsync(request, cancellationToken);
        await EnsureAssignedAdminExistsAsync(request.AssignedAdminUserId, cancellationToken);

        var entity = await dbContext.SecurityIncidentRecords
            .FirstOrDefaultAsync(x => x.Id == incidentId, cancellationToken)
            ?? throw new EntityNotFoundException("Incidente de seguranca", incidentId);

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.Severity = request.Severity;
        entity.Status = request.Status;
        entity.ContainsPersonalData = request.ContainsPersonalData;
        entity.AffectedDataSummary = request.AffectedDataSummary.Trim();
        entity.ImpactSummary = request.ImpactSummary.Trim();
        entity.MitigationSummary = request.MitigationSummary.Trim();
        entity.NotifyAnpd = request.NotifyAnpd;
        entity.NotifyDataSubjects = request.NotifyDataSubjects;
        entity.AssignedAdminUserId = NormalizeOptional(request.AssignedAdminUserId);
        entity.DetectedAtUtc = request.DetectedAtUtc;
        entity.OccurredAtUtc = request.OccurredAtUtc;
        entity.ContainedAtUtc = request.ContainedAtUtc
            ?? entity.ContainedAtUtc
            ?? (string.Equals(request.Status, "Contido", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.Status, "Resolvido", StringComparison.OrdinalIgnoreCase)
                ? DateTimeOffset.UtcNow
                : null);
        entity.ReportedToAnpdAtUtc = request.ReportedToAnpdAtUtc;
        entity.ReportedToDataSubjectsAtUtc = request.ReportedToDataSubjectsAtUtc;
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        dbContext.Update(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var adminNames = await GetAdminNamesAsync(cancellationToken);
        return MapSecurityIncident(entity, adminNames);
    }

    private int GetDefaultSubjectRequestDueDays()
    {
        var rawValue = configuration["Compliance:DataSubjectRequestDueDays"];
        return int.TryParse(rawValue, out var parsed) && parsed > 0 ? parsed : 15;
    }

    private async Task EnsureAssignedAdminExistsAsync(string? adminUserId, CancellationToken cancellationToken)
    {
        var normalizedAdminUserId = NormalizeOptional(adminUserId);
        if (normalizedAdminUserId is null)
        {
            return;
        }

        var exists = await dbContext.AdminUsers.AnyAsync(x => x.Id == normalizedAdminUserId, cancellationToken);
        if (!exists)
        {
            throw new EntityNotFoundException("Responsavel", normalizedAdminUserId);
        }
    }

    private async Task<IReadOnlyDictionary<string, string>> GetAdminNamesAsync(CancellationToken cancellationToken) =>
        await dbContext.AdminUsers.ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

    private static DataSubjectRequestDto MapDataSubjectRequest(
        DataSubjectRequest entity,
        IReadOnlyDictionary<string, string> adminNames) =>
        new(
            entity.Id,
            entity.SubjectName,
            entity.SubjectEmail,
            entity.SubjectDocument,
            entity.RequestType,
            entity.Channel,
            entity.Status,
            entity.Details,
            entity.ResolutionSummary,
            entity.IdentityVerified,
            entity.AssignedAdminUserId,
            ResolveAdminName(entity.AssignedAdminUserId, adminNames),
            entity.RequestedAtUtc,
            entity.DueAtUtc,
            entity.AcknowledgedAtUtc,
            entity.CompletedAtUtc,
            entity.UpdatedAtUtc);

    private static ProcessingActivityDto MapProcessingActivity(ProcessingActivityRecord entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.SystemName,
            entity.Purpose,
            entity.LegalBasis,
            entity.DataSubjectCategories,
            entity.PersonalDataCategories,
            entity.SharedWith,
            entity.RetentionPolicy,
            entity.SecurityMeasures,
            entity.OwnerArea,
            entity.InternationalTransfer,
            entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            entity.ReviewDueAtUtc);

    private static SecurityIncidentDto MapSecurityIncident(
        SecurityIncidentRecord entity,
        IReadOnlyDictionary<string, string> adminNames) =>
        new(
            entity.Id,
            entity.Title,
            entity.Description,
            entity.Severity,
            entity.Status,
            entity.ContainsPersonalData,
            entity.AffectedDataSummary,
            entity.ImpactSummary,
            entity.MitigationSummary,
            entity.NotifyAnpd,
            entity.NotifyDataSubjects,
            entity.AssignedAdminUserId,
            ResolveAdminName(entity.AssignedAdminUserId, adminNames),
            entity.DetectedAtUtc,
            entity.OccurredAtUtc,
            entity.ContainedAtUtc,
            entity.ReportedToAnpdAtUtc,
            entity.ReportedToDataSubjectsAtUtc,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

    private static string? ResolveAdminName(string? adminUserId, IReadOnlyDictionary<string, string> adminNames) =>
        adminUserId is not null && adminNames.TryGetValue(adminUserId, out var name) ? name : null;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
