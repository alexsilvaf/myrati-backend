using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Document).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DocumentType).Must(type => type is "CPF" or "CNPJ");
        RuleFor(x => x.Company).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Status).Must(status => status is "Ativo" or "Inativo");
    }
}

public sealed class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(25);
        RuleFor(x => x.Document).NotEmpty().MaximumLength(20);
        RuleFor(x => x.DocumentType).Must(type => type is "CPF" or "CNPJ");
        RuleFor(x => x.Company).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Status).Must(status => status is "Ativo" or "Inativo");
    }
}
