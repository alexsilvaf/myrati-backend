using FluentValidation;
using Myrati.Application.Contracts;

namespace Myrati.Application.Validation;

public sealed class UpsertProductPlanRequestValidator : AbstractValidator<UpsertProductPlanRequest>
{
    public UpsertProductPlanRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.MaxUsers).GreaterThan(0).When(x => x.MaxUsers.HasValue);
        RuleFor(x => x.MonthlyPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DevelopmentCost).GreaterThanOrEqualTo(0).When(x => x.DevelopmentCost.HasValue);
        RuleFor(x => x.MaintenanceCost).GreaterThanOrEqualTo(0).When(x => x.MaintenanceCost.HasValue);
        RuleFor(x => x.RevenueSharePercent).InclusiveBetween(0, 100).When(x => x.RevenueSharePercent.HasValue);
        RuleFor(x => x.MaintenanceProfitMargin).InclusiveBetween(0, 100).When(x => x.MaintenanceProfitMargin.HasValue);
    }
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(120);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.SalesStrategy).Must(BeValidSalesStrategy);
        RuleFor(x => x.Plans).NotEmpty();
        RuleForEach(x => x.Plans).SetValidator(new UpsertProductPlanRequestValidator());
        RuleFor(x => x).Custom(ValidatePlanPricing);
    }

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Inativo" or "Em desenvolvimento";

    private static bool BeValidSalesStrategy(string salesStrategy) =>
        salesStrategy is "subscription" or "development" or "revenue_share";

    private static void ValidatePlanPricing(CreateProductRequest request, ValidationContext<CreateProductRequest> context)
    {
        ProductValidationRules.ValidatePlanPricing(request.Status, request.SalesStrategy, request.Plans, context.AddFailure);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).MaximumLength(120);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.SalesStrategy).Must(BeValidSalesStrategy);
        RuleFor(x => x.Plans).NotEmpty();
        RuleForEach(x => x.Plans).SetValidator(new UpsertProductPlanRequestValidator());
        RuleFor(x => x).Custom(ValidatePlanPricing);
    }

    private static bool BeValidStatus(string status) =>
        status is "Ativo" or "Inativo" or "Em desenvolvimento";

    private static bool BeValidSalesStrategy(string salesStrategy) =>
        salesStrategy is "subscription" or "development" or "revenue_share";

    private static void ValidatePlanPricing(UpdateProductRequest request, ValidationContext<UpdateProductRequest> context)
    {
        ProductValidationRules.ValidatePlanPricing(request.Status, request.SalesStrategy, request.Plans, context.AddFailure);
    }
}

public sealed class ImportProductTaskRequestValidator : AbstractValidator<ImportProductTaskRequest>
{
    public ImportProductTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Column).Must(BeValidKanbanColumn);
        RuleFor(x => x.Priority).Must(BeValidPriority);
        RuleFor(x => x.Assignee).MaximumLength(160);
        RuleForEach(x => x.Tags).MaximumLength(30);
    }

    private static bool BeValidKanbanColumn(string column) =>
        column is "backlog" or "todo" or "in_progress" or "review" or "done";

    private static bool BeValidPriority(string priority) =>
        priority is "low" or "medium" or "high" or "critical";
}

public sealed class ImportProductSprintRequestValidator : AbstractValidator<ImportProductSprintRequest>
{
    public ImportProductSprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Status).Must(BeValidSprintStatus);
        RuleForEach(x => x.Tasks).SetValidator(new ImportProductTaskRequestValidator());
    }

    private static bool BeValidSprintStatus(string status) =>
        status is "Planejada" or "Ativa" or "Concluída";
}

public sealed class ImportProductBacklogRequestValidator : AbstractValidator<ImportProductBacklogRequest>
{
    public ImportProductBacklogRequestValidator()
    {
        RuleFor(x => x.Sprints).NotEmpty();
        RuleForEach(x => x.Sprints).SetValidator(new ImportProductSprintRequestValidator());
    }
}

public sealed class CreateProductSetupRequestValidator : AbstractValidator<CreateProductSetupRequest>
{
    public CreateProductSetupRequestValidator()
    {
        RuleFor(x => x.Product).NotNull().SetValidator(new CreateProductRequestValidator());
        When(
            x => x.InitialBacklog is not null,
            () =>
            {
                RuleFor(x => x.InitialBacklog!).SetValidator(new ImportProductBacklogRequestValidator());
                RuleFor(x => x)
                    .Must(x => x.Product.Status == "Em desenvolvimento")
                    .WithMessage("O assistente inicial só pode criar sprint e tarefas quando o produto estiver em 'Em desenvolvimento'.");
            });
    }
}

public sealed class CreateLicenseRequestValidator : AbstractValidator<CreateLicenseRequest>
{
    public CreateLicenseRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Plan).NotEmpty().MaximumLength(60);
        RuleFor(x => x.MonthlyValue).GreaterThan(0);
        RuleFor(x => x.DevelopmentCost).GreaterThan(0).When(x => x.DevelopmentCost.HasValue);
        RuleFor(x => x.RevenueSharePercent).InclusiveBetween(0, 100).When(x => x.RevenueSharePercent.HasValue);
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
        RuleFor(x => x.DevelopmentCost).GreaterThan(0).When(x => x.DevelopmentCost.HasValue);
        RuleFor(x => x.RevenueSharePercent).InclusiveBetween(0, 100).When(x => x.RevenueSharePercent.HasValue);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.ExpiryDate).NotEmpty();
    }
}

public sealed class CreateProductSprintRequestValidator : AbstractValidator<CreateProductSprintRequest>
{
    public CreateProductSprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Status).Must(BeValidSprintStatus);
    }

    private static bool BeValidSprintStatus(string status) =>
        status is "Planejada" or "Ativa" or "Concluída";
}

public sealed class UpdateProductSprintRequestValidator : AbstractValidator<UpdateProductSprintRequest>
{
    public UpdateProductSprintRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x.Status).Must(BeValidSprintStatus);
    }

    private static bool BeValidSprintStatus(string status) =>
        status is "Planejada" or "Ativa" or "Concluída";
}

public sealed class CreateProductTaskRequestValidator : AbstractValidator<CreateProductTaskRequest>
{
    public CreateProductTaskRequestValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Column).Must(BeValidKanbanColumn);
        RuleFor(x => x.Priority).Must(BeValidPriority);
        RuleFor(x => x.Assignee).MaximumLength(160);
        RuleForEach(x => x.Tags).MaximumLength(30);
    }

    private static bool BeValidKanbanColumn(string column) =>
        column is "backlog" or "todo" or "in_progress" or "review" or "done";

    private static bool BeValidPriority(string priority) =>
        priority is "low" or "medium" or "high" or "critical";
}

public sealed class UpdateProductTaskRequestValidator : AbstractValidator<UpdateProductTaskRequest>
{
    public UpdateProductTaskRequestValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Column).Must(BeValidKanbanColumn);
        RuleFor(x => x.Priority).Must(BeValidPriority);
        RuleFor(x => x.Assignee).MaximumLength(160);
        RuleForEach(x => x.Tags).MaximumLength(30);
    }

    private static bool BeValidKanbanColumn(string column) =>
        column is "backlog" or "todo" or "in_progress" or "review" or "done";

    private static bool BeValidPriority(string priority) =>
        priority is "low" or "medium" or "high" or "critical";
}

public sealed class CreateProductExpenseRequestValidator : AbstractValidator<CreateProductExpenseRequest>
{
    public CreateProductExpenseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Category).Must(BeValidExpenseCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Recurrence).Must(BeValidExpenseRecurrence);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }

    private static bool BeValidExpenseCategory(string category) =>
        category is "hosting" or "domain" or "tools" or "apis" or "infrastructure" or "licenses" or "other";

    private static bool BeValidExpenseRecurrence(string recurrence) =>
        recurrence is "monthly" or "annual" or "one_time";
}

public sealed class UpdateProductExpenseRequestValidator : AbstractValidator<UpdateProductExpenseRequest>
{
    public UpdateProductExpenseRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Category).Must(BeValidExpenseCategory);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Recurrence).Must(BeValidExpenseRecurrence);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }

    private static bool BeValidExpenseCategory(string category) =>
        category is "hosting" or "domain" or "tools" or "apis" or "infrastructure" or "licenses" or "other";

    private static bool BeValidExpenseRecurrence(string recurrence) =>
        recurrence is "monthly" or "annual" or "one_time";
}

public sealed class ProductPermissionSetDtoValidator : AbstractValidator<ProductPermissionSetDto>
{
    public ProductPermissionSetDtoValidator()
    {
        RuleFor(x => x).NotNull();
    }
}

public sealed class ProductCollaboratorPermissionsDtoValidator : AbstractValidator<ProductCollaboratorPermissionsDto>
{
    public ProductCollaboratorPermissionsDtoValidator()
    {
        RuleFor(x => x.Tasks).SetValidator(new ProductPermissionSetDtoValidator());
        RuleFor(x => x.Sprints).SetValidator(new ProductPermissionSetDtoValidator());
        RuleFor(x => x.Licenses).SetValidator(new ProductPermissionSetDtoValidator());
        RuleFor(x => x.Plans).SetValidator(new ProductPermissionSetDtoValidator());
        RuleFor(x => x.Product).SetValidator(new ProductPermissionSetDtoValidator());
    }
}

public sealed class AddProductCollaboratorRequestValidator : AbstractValidator<AddProductCollaboratorRequest>
{
    public AddProductCollaboratorRequestValidator()
    {
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.Permissions).SetValidator(new ProductCollaboratorPermissionsDtoValidator());
    }
}

public sealed class UpdateProductCollaboratorRequestValidator : AbstractValidator<UpdateProductCollaboratorRequest>
{
    public UpdateProductCollaboratorRequestValidator()
    {
        RuleFor(x => x.Permissions).SetValidator(new ProductCollaboratorPermissionsDtoValidator());
    }
}

internal static class ProductValidationRules
{
    public static void ValidatePlanPricing(
        string status,
        string salesStrategy,
        IReadOnlyCollection<UpsertProductPlanRequest> plans,
        Action<string, string> addFailure)
    {
        var allowDraftPlan = string.Equals(status, "Em desenvolvimento", StringComparison.Ordinal);

        foreach (var plan in plans)
        {
            if (plan.MaxUsers.HasValue && plan.MaxUsers.Value <= 0)
            {
                addFailure(nameof(plan.MaxUsers), $"O plano '{plan.Name}' deve definir maxUsers maior que zero ou deixar o campo nulo para ilimitado.");
            }

            switch (salesStrategy)
            {
                case "subscription":
                    if (!allowDraftPlan && plan.MonthlyPrice <= 0)
                    {
                        addFailure(nameof(plan.MonthlyPrice), $"O plano '{plan.Name}' precisa ter preço mensal maior que zero.");
                    }

                    break;
                case "development":
                    if (!allowDraftPlan && (!plan.DevelopmentCost.HasValue || plan.DevelopmentCost.Value <= 0))
                    {
                        addFailure(nameof(plan.DevelopmentCost), $"O plano '{plan.Name}' precisa ter custo de desenvolvimento.");
                    }

                    break;
                case "revenue_share":
                    if (!allowDraftPlan && (!plan.RevenueSharePercent.HasValue || plan.RevenueSharePercent.Value <= 0))
                    {
                        addFailure(nameof(plan.RevenueSharePercent), $"O plano '{plan.Name}' precisa ter percentual de participação.");
                    }

                    break;
            }
        }
    }
}
