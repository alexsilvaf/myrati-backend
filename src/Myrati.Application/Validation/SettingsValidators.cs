using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(x => x.CompanyInfo.Name).NotEmpty();
        RuleFor(x => x.CompanyInfo.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.CompanyInfo.Cnpj).NotEmpty();
        RuleFor(x => x.Regional.Language).NotEmpty();
        RuleFor(x => x.Regional.Timezone).NotEmpty();
        RuleFor(x => x.Security.SessionTimeout).NotEmpty();
    }
}

public sealed class CreateApiKeyRequestValidator : AbstractValidator<CreateApiKeyRequest>
{
    public CreateApiKeyRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Environment).Must(value => value is "production" or "staging");
    }
}

public sealed class CreateTeamMemberRequestValidator : AbstractValidator<CreateTeamMemberRequest>
{
    public CreateTeamMemberRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).Must(role => role is "Super Admin" or "Admin" or "Viewer" or "Desenvolvedor");
    }
}

public sealed class UpdateTeamMemberRequestValidator : AbstractValidator<UpdateTeamMemberRequest>
{
    public UpdateTeamMemberRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role).Must(role => role is "Super Admin" or "Admin" or "Viewer" or "Desenvolvedor");
        RuleFor(x => x.Status).Must(status => status is "Ativo" or "Convite Pendente");
    }
}
