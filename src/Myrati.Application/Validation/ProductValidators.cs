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
        RuleFor(x => x.DevelopmentCost).GreaterThanOrEqualTo(0).When(x => x.DevelopmentCost.HasValue);
        RuleFor(x => x.MaintenanceCost).GreaterThanOrEqualTo(0).When(x => x.MaintenanceCost.HasValue);
        RuleFor(x => x.RevenueSharePercent).InclusiveBetween(0, 100).When(x => x.RevenueSharePercent.HasValue);
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
        RuleFor(x => x.SalesStrategy).Must(BeValidSalesStrategy);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
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
        ProductValidationRules.ValidatePlanPricing(request.SalesStrategy, request.Plans, context.AddFailure);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Status).Must(BeValidStatus);
        RuleFor(x => x.SalesStrategy).Must(BeValidSalesStrategy);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(30);
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
        ProductValidationRules.ValidatePlanPricing(request.SalesStrategy, request.Plans, context.AddFailure);
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

internal static class ProductValidationRules
{
    public static void ValidatePlanPricing(
        string salesStrategy,
        IReadOnlyCollection<UpsertProductPlanRequest> plans,
        Action<string, string> addFailure)
    {
        foreach (var plan in plans)
        {
            switch (salesStrategy)
            {
                case "subscription":
                    if (plan.MonthlyPrice <= 0)
                    {
                        addFailure(nameof(plan.MonthlyPrice), $"O plano '{plan.Name}' precisa ter preço mensal maior que zero.");
                    }

                    break;
                case "development":
                    if (!plan.DevelopmentCost.HasValue || plan.DevelopmentCost.Value <= 0)
                    {
                        addFailure(nameof(plan.DevelopmentCost), $"O plano '{plan.Name}' precisa ter custo de desenvolvimento.");
                    }

                    if (!plan.MaintenanceCost.HasValue || plan.MaintenanceCost.Value <= 0)
                    {
                        addFailure(nameof(plan.MaintenanceCost), $"O plano '{plan.Name}' precisa ter custo de manutenção.");
                    }

                    break;
                case "revenue_share":
                    if (!plan.MaintenanceCost.HasValue || plan.MaintenanceCost.Value <= 0)
                    {
                        addFailure(nameof(plan.MaintenanceCost), $"O plano '{plan.Name}' precisa ter custo de manutenção.");
                    }

                    if (!plan.RevenueSharePercent.HasValue || plan.RevenueSharePercent.Value <= 0)
                    {
                        addFailure(nameof(plan.RevenueSharePercent), $"O plano '{plan.Name}' precisa ter percentual de participação.");
                    }

                    break;
            }
        }
    }
}
