using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class CreateDataSubjectRequestRequestValidator : AbstractValidator<CreateDataSubjectRequestRequest>
{
    public CreateDataSubjectRequestRequestValidator()
    {
        RuleFor(x => x.SubjectName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.SubjectEmail).NotEmpty().EmailAddress().MaximumLength(160);
        RuleFor(x => x.SubjectDocument).MaximumLength(40);
        RuleFor(x => x.RequestType).Must(BeDataSubjectRequestType);
        RuleFor(x => x.Channel).Must(BeContactChannel);
        RuleFor(x => x.Details).NotEmpty().MaximumLength(3000);
    }

    private static bool BeDataSubjectRequestType(string value) =>
        value is "Acesso" or "Correcao" or "Portabilidade" or "Eliminacao" or "Anonimizacao" or "Bloqueio" or "Revogacao" or "Oposicao" or "Revisao";

    private static bool BeContactChannel(string value) =>
        value is "Email" or "Portal" or "Telefone" or "WhatsApp" or "Presencial" or "Suporte";
}

public sealed class UpdateDataSubjectRequestRequestValidator : AbstractValidator<UpdateDataSubjectRequestRequest>
{
    public UpdateDataSubjectRequestRequestValidator()
    {
        RuleFor(x => x.Status).Must(BeDataSubjectRequestStatus);
        RuleFor(x => x.Details).NotEmpty().MaximumLength(3000);
        RuleFor(x => x.ResolutionSummary).MaximumLength(3000);
    }

    private static bool BeDataSubjectRequestStatus(string value) =>
        value is "Recebida" or "Em analise" or "Em atendimento" or "Concluida" or "Negada" or "Arquivada";
}

public sealed class CreateProcessingActivityRequestValidator : AbstractValidator<CreateProcessingActivityRequest>
{
    public CreateProcessingActivityRequestValidator()
    {
        Include(new ProcessingActivityRules<CreateProcessingActivityRequest>());
    }
}

public sealed class UpdateProcessingActivityRequestValidator : AbstractValidator<UpdateProcessingActivityRequest>
{
    public UpdateProcessingActivityRequestValidator()
    {
        Include(new ProcessingActivityRules<UpdateProcessingActivityRequest>());
    }
}

public sealed class CreateSecurityIncidentRequestValidator : AbstractValidator<CreateSecurityIncidentRequest>
{
    public CreateSecurityIncidentRequestValidator()
    {
        Include(new SecurityIncidentRules<CreateSecurityIncidentRequest>());
    }
}

public sealed class UpdateSecurityIncidentRequestValidator : AbstractValidator<UpdateSecurityIncidentRequest>
{
    public UpdateSecurityIncidentRequestValidator()
    {
        Include(new SecurityIncidentRules<UpdateSecurityIncidentRequest>());
    }
}

file sealed class ProcessingActivityRules<T> : AbstractValidator<T>
    where T : class
{
    public ProcessingActivityRules()
    {
        RuleForString("Name", 160);
        RuleForString("SystemName", 120);
        RuleForString("Purpose", 2000);
        RuleForString("LegalBasis", 120);
        RuleForString("DataSubjectCategories", 2000);
        RuleForString("PersonalDataCategories", 2000);
        RuleForString("SharedWith", 2000);
        RuleForString("RetentionPolicy", 1000);
        RuleForString("SecurityMeasures", 2000);
        RuleForString("OwnerArea", 120);
        RuleFor(x => ComplianceValidationHelpers.GetStringValue(x, "Status"))
            .Must(value => value is "Ativa" or "Em revisao" or "Arquivada");
    }

    private void RuleForString(string propertyName, int maximumLength)
    {
        RuleFor(x => ComplianceValidationHelpers.GetStringValue(x, propertyName))
            .NotEmpty()
            .MaximumLength(maximumLength);
    }
}

file sealed class SecurityIncidentRules<T> : AbstractValidator<T>
    where T : class
{
    public SecurityIncidentRules()
    {
        RuleForString("Title", 200);
        RuleForString("Description", 3000);
        RuleFor(x => ComplianceValidationHelpers.GetStringValue(x, "Severity"))
            .Must(value => value is "Baixa" or "Media" or "Alta" or "Critica");
        RuleFor(x => ComplianceValidationHelpers.GetStringValue(x, "Status"))
            .Must(value => value is "Aberto" or "Em investigacao" or "Contido" or "Comunicado" or "Resolvido");
        RuleForString("AffectedDataSummary", 2000);
        RuleForString("ImpactSummary", 2000);
        RuleForString("MitigationSummary", 2000);
    }

    private void RuleForString(string propertyName, int maximumLength)
    {
        RuleFor(x => ComplianceValidationHelpers.GetStringValue(x, propertyName))
            .NotEmpty()
            .MaximumLength(maximumLength);
    }
}

file static class ComplianceValidationHelpers
{
    public static string GetStringValue<T>(T instance, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Propriedade '{propertyName}' não encontrada em '{typeof(T).Name}'.");
        return property.GetValue(instance)?.ToString() ?? string.Empty;
    }
}
