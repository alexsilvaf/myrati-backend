using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class CreateCompanyCostRequestValidator : AbstractValidator<CreateCompanyCostRequest>
{
    public CreateCompanyCostRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).Must(BeValidCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Recurrence).Must(BeValidRecurrence);
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(160);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Status).Must(BeValidStatus);
    }

    private static bool BeValidCategory(string category) =>
        category is "subscriptions" or "infrastructure" or "cloud" or "domains" or "security" or "tools" or "other";

    private static bool BeValidRecurrence(string recurrence) =>
        recurrence is "monthly" or "annual" or "one_time";

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Cancelado" or "Pausado";
}

public sealed class UpdateCompanyCostRequestValidator : AbstractValidator<UpdateCompanyCostRequest>
{
    public UpdateCompanyCostRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).Must(BeValidCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Recurrence).Must(BeValidRecurrence);
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(160);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Status).Must(BeValidStatus);
    }

    private static bool BeValidCategory(string category) =>
        category is "subscriptions" or "infrastructure" or "cloud" or "domains" or "security" or "tools" or "other";

    private static bool BeValidRecurrence(string recurrence) =>
        recurrence is "monthly" or "annual" or "one_time";

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Cancelado" or "Pausado";
}
