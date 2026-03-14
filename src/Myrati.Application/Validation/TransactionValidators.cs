using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class CreateCashTransactionRequestValidator : AbstractValidator<CreateCashTransactionRequest>
{
    public CreateCashTransactionRequestValidator()
    {
        RuleFor(x => x.Type).Must(BeValidType);
        RuleFor(x => x.Category).Must(BeValidCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(240);
        RuleFor(x => x.ReferenceProductId)
            .MaximumLength(40)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceProductId));
        RuleFor(x => x.Date).NotEmpty();
    }

    private static bool BeValidType(string type) =>
        type is "deposit" or "withdrawal";

    private static bool BeValidCategory(string category) =>
        category is
            "license_revenue"
            or "development_payment"
            or "revenue_share"
            or "client_payment"
            or "salary"
            or "tax"
            or "supplier"
            or "transfer"
            or "refund"
            or "other";
}

public sealed class UpdateCashTransactionRequestValidator : AbstractValidator<UpdateCashTransactionRequest>
{
    public UpdateCashTransactionRequestValidator()
    {
        RuleFor(x => x.Type).Must(BeValidType);
        RuleFor(x => x.Category).Must(BeValidCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(240);
        RuleFor(x => x.ReferenceProductId)
            .MaximumLength(40)
            .When(x => !string.IsNullOrWhiteSpace(x.ReferenceProductId));
        RuleFor(x => x.Date).NotEmpty();
    }

    private static bool BeValidType(string type) =>
        type is "deposit" or "withdrawal";

    private static bool BeValidCategory(string category) =>
        category is
            "license_revenue"
            or "development_payment"
            or "revenue_share"
            or "client_payment"
            or "salary"
            or "tax"
            or "supplier"
            or "transfer"
            or "refund"
            or "other";
}
