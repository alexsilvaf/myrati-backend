using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    public ContactRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Subject).MaximumLength(120);
        RuleFor(x => x.Company).MaximumLength(160);
    }
}

public sealed class LicenseActivationRequestValidator : AbstractValidator<LicenseActivationRequest>
{
    public LicenseActivationRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(40);
        RuleFor(x => x.LicenseKey).NotEmpty().MaximumLength(64);
    }
}
