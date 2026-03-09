using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class UpsertProductPlanRequestValidator : AbstractValidator<UpsertProductPlanRequest>
{
    public UpsertProductPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.MaxUsers).GreaterThan(0);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Plans).NotEmpty();
        RuleForEach(x => x.Plans).SetValidator(new UpsertProductPlanRequestValidator());
    }

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Inativo" or "Em desenvolvimento";
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Plans).NotEmpty();
        RuleForEach(x => x.Plans).SetValidator(new UpsertProductPlanRequestValidator());
    }

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Inativo" or "Em desenvolvimento";
}

public sealed class CreateLicenseRequestValidator : AbstractValidator<CreateLicenseRequest>
{
    public CreateLicenseRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Plan).NotEmpty().MaximumLength(60);
        RuleFor(x => x.MonthlyValue).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).NotEmpty();
    }
}

public sealed class UpdateLicenseRequestValidator : AbstractValidator<UpdateLicenseRequest>
{
    public UpdateLicenseRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Plan).NotEmpty().MaximumLength(60);
        RuleFor(x => x.MonthlyValue).GreaterThan(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).NotEmpty();
    }
}
