using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Application.Realtime;
using Myrati.Domain.Products;

namespace Myrati.Application.Services;

public sealed class ProductsService(
    IMyratiDbContext dbContext,
    IValidator<CreateProductRequest> createProductValidator,
    IValidator<UpdateProductRequest> updateProductValidator,
    IValidator<CreateLicenseRequest> createLicenseValidator,
    IValidator<UpdateLicenseRequest> updateLicenseValidator,
    IValidator<CreateProductSprintRequest> createSprintValidator,
    IValidator<UpdateProductSprintRequest> updateSprintValidator,
    IValidator<CreateProductTaskRequest> createTaskValidator,
    IValidator<UpdateProductTaskRequest> updateTaskValidator,
    IRealtimeEventPublisher realtimeEventPublisher) : IProductsService
{
    private static readonly string[] KanbanColumnOrder = ["backlog", "todo", "in_progress", "review", "done"];

    public async Task<IReadOnlyCollection<ProductSummaryDto>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await dbContext.Products
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        var plans = await dbContext.ProductPlans.ToListAsync(cancellationToken);
        var licenses = await dbContext.Licenses.ToListAsync(cancellationToken);

        return products
            .Select(product => MapProductSummary(
                product,
                plans.Where(x => x.ProductId == product.Id),
                licenses.Where(x => x.ProductId == product.Id)))
            .ToArray();
    }

    public async Task<ProductDetailDto> GetProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductEntityAsync(productId, cancellationToken);

        var plans = await dbContext.ProductPlans
            .Where(x => x.ProductId == product.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
        var licenses = await dbContext.Licenses
            .Where(x => x.ProductId == product.Id)
            .OrderByDescending(x => x.Status == "Ativa")
            .ThenBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);
        var clients = await dbContext.Clients.ToListAsync(cancellationToken);
        var kanban = await BuildKanbanAsync(product, cancellationToken);

        return MapProductDetail(product, plans, licenses, clients, kanban);
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await createProductValidator.ValidateRequestAsync(request, cancellationToken);

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameInUse = await dbContext.Products.AnyAsync(
            x => x.Name.ToLower() == normalizedName,
            cancellationToken);

        if (nameInUse)
        {
            throw new ConflictException($"Já existe um produto com o nome '{request.Name}'.");
        }

        var productId = IdGenerator.NextPrefixedId(
            "PRD-",
            await dbContext.Products.Select(x => x.Id).ToListAsync(cancellationToken));

        var product = new Product
        {
            Id = productId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Status = request.Status,
            SalesStrategy = request.SalesStrategy,
            Version = request.Version.Trim(),
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.AddAsync(product, cancellationToken);
        await ReplacePlansAsync(productId, request.Plans, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetProductAsync(productId, cancellationToken);
        await PublishBackofficeEventAsync("product.created", response, cancellationToken);
        return response;
    }

    public async Task<ProductDetailDto> UpdateProductAsync(
        string productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateProductValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var nameInUse = await dbContext.Products.AnyAsync(
            x => x.Id != productId && x.Name.ToLower() == normalizedName,
            cancellationToken);

        if (nameInUse)
        {
            throw new ConflictException($"Já existe um produto com o nome '{request.Name}'.");
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description.Trim();
        product.Category = request.Category.Trim();
        product.Status = request.Status;
        product.SalesStrategy = request.SalesStrategy;
        product.Version = request.Version.Trim();
        dbContext.Update(product);

        var existingPlans = await dbContext.ProductPlans
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var plan in existingPlans)
        {
            dbContext.Remove(plan);
        }

        await ReplacePlansAsync(productId, request.Plans, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetProductAsync(productId, cancellationToken);
        await PublishBackofficeEventAsync("product.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductEntityAsync(productId, cancellationToken);

        var hasLicenses = await dbContext.Licenses.AnyAsync(x => x.ProductId == productId, cancellationToken);
        if (hasLicenses)
        {
            throw new ConflictException("Não é possível remover um produto que ainda possui licenças vinculadas.");
        }

        var connectedUsers = await dbContext.ConnectedUsers
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var tasks = await dbContext.ProductTasks
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var sprints = await dbContext.ProductSprints
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);
        var plans = await dbContext.ProductPlans
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var connectedUser in connectedUsers)
        {
            dbContext.Remove(connectedUser);
        }

        foreach (var task in tasks)
        {
            dbContext.Remove(task);
        }

        foreach (var sprint in sprints)
        {
            dbContext.Remove(sprint);
        }

        foreach (var plan in plans)
        {
            dbContext.Remove(plan);
        }

        dbContext.Remove(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "product.deleted",
            new { productId = product.Id, product.Name },
            cancellationToken);
    }

    public async Task<LicenseDto> CreateLicenseAsync(
        string productId,
        CreateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        await createLicenseValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == request.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", request.ClientId);

        if (client.Status != "Ativo")
        {
            throw new ConflictException("A licença só pode ser vinculada a clientes ativos.");
        }

        var plan = await GetPlanAsync(productId, request.Plan, cancellationToken);
        var pricing = ResolveLicensePricing(product.SalesStrategy, plan, request.MonthlyValue, request.DevelopmentCost, request.RevenueSharePercent);
        var startDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        var expiryDate = RequestValidation.ParseIsoDate(request.ExpiryDate, nameof(request.ExpiryDate));
        ValidateLicenseDates(startDate, expiryDate);

        var license = new License
        {
            Id = await GenerateUniqueLicenseKeyAsync(cancellationToken),
            ClientId = client.Id,
            ProductId = product.Id,
            Plan = plan.Name,
            MaxUsers = plan.MaxUsers,
            ActiveUsers = 0,
            Status = DetermineLicenseStatus(startDate, expiryDate),
            StartDate = startDate,
            ExpiryDate = expiryDate,
            MonthlyValue = pricing.MonthlyValue,
            DevelopmentCost = pricing.DevelopmentCost,
            RevenueSharePercent = pricing.RevenueSharePercent
        };

        await dbContext.AddAsync(license, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapLicense(license, client.Company, product.Name);
        await PublishBackofficeEventAsync("license.created", response, cancellationToken);
        return response;
    }

    public async Task<LicenseDto> UpdateLicenseAsync(
        string licenseId,
        UpdateLicenseRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateLicenseValidator.ValidateRequestAsync(request, cancellationToken);

        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == request.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", request.ClientId);
        var product = await GetProductEntityAsync(license.ProductId, cancellationToken);
        var plan = await GetPlanAsync(product.Id, request.Plan, cancellationToken);
        var pricing = ResolveLicensePricing(product.SalesStrategy, plan, request.MonthlyValue, request.DevelopmentCost, request.RevenueSharePercent);

        var startDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        var expiryDate = RequestValidation.ParseIsoDate(request.ExpiryDate, nameof(request.ExpiryDate));
        ValidateLicenseDates(startDate, expiryDate);

        license.ClientId = client.Id;
        license.Plan = plan.Name;
        license.MaxUsers = plan.MaxUsers;
        license.StartDate = startDate;
        license.ExpiryDate = expiryDate;
        license.MonthlyValue = pricing.MonthlyValue;
        license.DevelopmentCost = pricing.DevelopmentCost;
        license.RevenueSharePercent = pricing.RevenueSharePercent;
        if (license.Status != "Suspensa")
        {
            license.Status = DetermineLicenseStatus(startDate, expiryDate);
        }

        dbContext.Update(license);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapLicense(license, client.Company, product.Name);
        await PublishBackofficeEventAsync("license.updated", response, cancellationToken);
        return response;
    }

    public async Task<LicenseDto> SuspendLicenseAsync(string licenseId, CancellationToken cancellationToken = default)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);

        license.Status = "Suspensa";
        dbContext.Update(license);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetLicenseAsync(licenseId, cancellationToken);
        await PublishBackofficeEventAsync("license.suspended", response, cancellationToken);
        return response;
    }

    public async Task<LicenseDto> ReactivateLicenseAsync(string licenseId, CancellationToken cancellationToken = default)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);

        license.Status = DetermineLicenseStatus(license.StartDate, license.ExpiryDate);
        dbContext.Update(license);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetLicenseAsync(licenseId, cancellationToken);
        await PublishBackofficeEventAsync("license.reactivated", response, cancellationToken);
        return response;
    }

    public async Task DeleteLicenseAsync(string licenseId, CancellationToken cancellationToken = default)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);

        dbContext.Remove(license);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync(
            "license.deleted",
            new { licenseId = license.Id, license.ProductId, license.ClientId },
            cancellationToken);
    }

    public async Task<ProductKanbanDto> GetKanbanAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductEntityAsync(productId, cancellationToken);
        return await BuildKanbanAsync(product, cancellationToken);
    }

    public async Task<ProductSprintDto> CreateSprintAsync(
        string productId,
        CreateProductSprintRequest request,
        CancellationToken cancellationToken = default)
    {
        await createSprintValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        var startDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        var endDate = RequestValidation.ParseIsoDate(request.EndDate, nameof(request.EndDate));
        ValidateSprintDates(startDate, endDate);

        await UpdateActiveSprintStateAsync(productId, request.Status, null, cancellationToken);

        var sprint = new ProductSprint
        {
            Id = IdGenerator.NextPrefixedId(
                "SPR-",
                await dbContext.ProductSprints.Select(x => x.Id).ToListAsync(cancellationToken)),
            ProductId = productId,
            Name = request.Name.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = request.Status,
            SortOrder = await GetNextSprintSortOrderAsync(productId, cancellationToken)
        };

        await dbContext.AddAsync(sprint, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapSprint(sprint);
        await PublishBackofficeEventAsync("sprint.created", response, cancellationToken);
        return response;
    }

    public async Task<ProductSprintDto> UpdateSprintAsync(
        string productId,
        string sprintId,
        UpdateProductSprintRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateSprintValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        var sprint = await GetSprintAsync(productId, sprintId, cancellationToken);
        var startDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        var endDate = RequestValidation.ParseIsoDate(request.EndDate, nameof(request.EndDate));
        ValidateSprintDates(startDate, endDate);

        await UpdateActiveSprintStateAsync(productId, request.Status, sprintId, cancellationToken);

        sprint.Name = request.Name.Trim();
        sprint.StartDate = startDate;
        sprint.EndDate = endDate;
        sprint.Status = request.Status;
        dbContext.Update(sprint);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapSprint(sprint);
        await PublishBackofficeEventAsync("sprint.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteSprintAsync(string productId, string sprintId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        var sprint = await GetSprintAsync(productId, sprintId, cancellationToken);
        var hasTasks = await dbContext.ProductTasks.AnyAsync(x => x.SprintId == sprintId, cancellationToken);
        if (hasTasks)
        {
            throw new ConflictException("Não é possível excluir uma sprint que ainda possui tarefas vinculadas.");
        }

        dbContext.Remove(sprint);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync("sprint.deleted", new { sprint.Id, sprint.ProductId, sprint.Name }, cancellationToken);
    }

    public async Task<ProductTaskDto> CreateTaskAsync(
        string productId,
        CreateProductTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await createTaskValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        _ = await GetSprintAsync(productId, request.SprintId, cancellationToken);

        var task = new ProductTask
        {
            Id = IdGenerator.NextPrefixedId(
                "TSK-",
                await dbContext.ProductTasks.Select(x => x.Id).ToListAsync(cancellationToken)),
            ProductId = productId,
            SprintId = request.SprintId,
            Title = request.Title.Trim(),
            Description = (request.Description ?? string.Empty).Trim(),
            Column = request.Column,
            Priority = request.Priority,
            Assignee = (request.Assignee ?? string.Empty).Trim(),
            TagsSerialized = SerializeTags(request.Tags),
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SortOrder = await GetNextTaskSortOrderAsync(productId, request.SprintId, request.Column, cancellationToken)
        };

        await dbContext.AddAsync(task, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapTask(task);
        await PublishBackofficeEventAsync("task.created", response, cancellationToken);
        return response;
    }

    public async Task<ProductTaskDto> UpdateTaskAsync(
        string productId,
        string taskId,
        UpdateProductTaskRequest request,
        CancellationToken cancellationToken = default)
    {
        await updateTaskValidator.ValidateRequestAsync(request, cancellationToken);

        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        var task = await GetTaskAsync(productId, taskId, cancellationToken);
        _ = await GetSprintAsync(productId, request.SprintId, cancellationToken);

        var movedAcrossBoard = !string.Equals(task.SprintId, request.SprintId, StringComparison.Ordinal)
            || !string.Equals(task.Column, request.Column, StringComparison.Ordinal);

        task.SprintId = request.SprintId;
        task.Title = request.Title.Trim();
        task.Description = (request.Description ?? string.Empty).Trim();
        task.Column = request.Column;
        task.Priority = request.Priority;
        task.Assignee = (request.Assignee ?? string.Empty).Trim();
        task.TagsSerialized = SerializeTags(request.Tags);
        if (movedAcrossBoard)
        {
            task.SortOrder = await GetNextTaskSortOrderAsync(productId, request.SprintId, request.Column, cancellationToken);
        }

        dbContext.Update(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = MapTask(task);
        await PublishBackofficeEventAsync(movedAcrossBoard ? "task.moved" : "task.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteTaskAsync(string productId, string taskId, CancellationToken cancellationToken = default)
    {
        var product = await GetProductEntityAsync(productId, cancellationToken);
        EnsureProductSupportsKanban(product);

        var task = await GetTaskAsync(productId, taskId, cancellationToken);
        dbContext.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PublishBackofficeEventAsync("task.deleted", new { task.Id, task.ProductId, task.SprintId, task.Title }, cancellationToken);
    }

    private async Task ReplacePlansAsync(
        string productId,
        IReadOnlyCollection<UpsertProductPlanRequest> plans,
        CancellationToken cancellationToken)
    {
        var planIndex = 1;
        foreach (var plan in plans)
        {
            await dbContext.AddAsync(new ProductPlan
            {
                Id = IdGenerator.CreatePlanId(productId, planIndex++),
                ProductId = productId,
                Name = plan.Name.Trim(),
                MaxUsers = plan.MaxUsers,
                MonthlyPrice = plan.MonthlyPrice,
                DevelopmentCost = NormalizeOptionalMoney(plan.DevelopmentCost),
                MaintenanceCost = NormalizeOptionalMoney(plan.MaintenanceCost),
                RevenueSharePercent = NormalizeOptionalPercent(plan.RevenueSharePercent)
            }, cancellationToken);
        }
    }

    private async Task<Product> GetProductEntityAsync(string productId, CancellationToken cancellationToken) =>
        await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
        ?? throw new EntityNotFoundException("Produto", productId);

    private async Task<LicenseDto> GetLicenseAsync(string licenseId, CancellationToken cancellationToken)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == license.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", license.ClientId);
        var product = await GetProductEntityAsync(license.ProductId, cancellationToken);

        return MapLicense(license, client.Company, product.Name);
    }

    private async Task<ProductPlan> GetPlanAsync(string productId, string planName, CancellationToken cancellationToken)
    {
        var normalizedPlanName = planName.Trim().ToLowerInvariant();
        var plan = await dbContext.ProductPlans
            .FirstOrDefaultAsync(
                x => x.ProductId == productId && x.Name.ToLower() == normalizedPlanName,
                cancellationToken);

        return plan ?? throw new ValidationException(
            [new ValidationFailure(nameof(planName), $"O plano '{planName}' não existe para este produto.")]);
    }

    private async Task<ProductSprint> GetSprintAsync(string productId, string sprintId, CancellationToken cancellationToken)
    {
        var sprint = await dbContext.ProductSprints
            .FirstOrDefaultAsync(x => x.Id == sprintId && x.ProductId == productId, cancellationToken);

        return sprint ?? throw new EntityNotFoundException("Sprint", sprintId);
    }

    private async Task<ProductTask> GetTaskAsync(string productId, string taskId, CancellationToken cancellationToken)
    {
        var task = await dbContext.ProductTasks
            .FirstOrDefaultAsync(x => x.Id == taskId && x.ProductId == productId, cancellationToken);

        return task ?? throw new EntityNotFoundException("Tarefa", taskId);
    }

    private async Task<string> GenerateUniqueLicenseKeyAsync(CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.Licenses.Select(x => x.Id).ToListAsync(cancellationToken);
        var candidate = IdGenerator.GenerateLicenseKey();

        while (existingIds.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            candidate = IdGenerator.GenerateLicenseKey();
        }

        return candidate;
    }

    private async Task<ProductKanbanDto> BuildKanbanAsync(Product product, CancellationToken cancellationToken)
    {
        var assignees = await dbContext.AdminUsers
            .Where(x => x.Status == "Ativo")
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        if (product.Status != "Em desenvolvimento")
        {
            return new ProductKanbanDto([], [], assignees);
        }

        var sprints = await dbContext.ProductSprints
            .Where(x => x.ProductId == product.Id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartDate)
            .ToListAsync(cancellationToken);
        var tasks = await dbContext.ProductTasks
            .Where(x => x.ProductId == product.Id)
            .ToListAsync(cancellationToken);

        return new ProductKanbanDto(
            sprints.Select(MapSprint).ToArray(),
            tasks
                .OrderBy(x => GetKanbanColumnOrder(x.Column))
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedDate)
                .Select(MapTask)
                .ToArray(),
            assignees);
    }

    private async Task<int> GetNextSprintSortOrderAsync(string productId, CancellationToken cancellationToken)
    {
        var currentMax = await dbContext.ProductSprints
            .Where(x => x.ProductId == productId)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (currentMax ?? 0) + 1;
    }

    private async Task<int> GetNextTaskSortOrderAsync(
        string productId,
        string sprintId,
        string column,
        CancellationToken cancellationToken)
    {
        var currentMax = await dbContext.ProductTasks
            .Where(x => x.ProductId == productId && x.SprintId == sprintId && x.Column == column)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync(cancellationToken);

        return (currentMax ?? 0) + 1;
    }

    private async Task UpdateActiveSprintStateAsync(
        string productId,
        string requestedStatus,
        string? excludeSprintId,
        CancellationToken cancellationToken)
    {
        if (requestedStatus != "Ativa")
        {
            return;
        }

        var activeSprints = await dbContext.ProductSprints
            .Where(x => x.ProductId == productId && x.Status == "Ativa" && x.Id != excludeSprintId)
            .ToListAsync(cancellationToken);

        foreach (var sprint in activeSprints)
        {
            sprint.Status = "Planejada";
            dbContext.Update(sprint);
        }
    }

    private static void EnsureProductSupportsKanban(Product product)
    {
        if (product.Status != "Em desenvolvimento")
        {
            throw new ConflictException("O kanban só pode ser alterado em produtos com status 'Em desenvolvimento'.");
        }
    }

    private static void ValidateLicenseDates(DateOnly startDate, DateOnly expiryDate)
    {
        if (expiryDate <= startDate)
        {
            throw new ValidationException(
                [new ValidationFailure("ExpiryDate", "A data de expiração deve ser maior que a data inicial.")]);
        }
    }

    private static void ValidateSprintDates(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new ValidationException(
                [new ValidationFailure("EndDate", "A data de término da sprint deve ser maior que a data inicial.")]);
        }
    }

    private static string DetermineLicenseStatus(DateOnly startDate, DateOnly expiryDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (startDate > today)
        {
            return "Pendente";
        }

        if (expiryDate < today)
        {
            return "Expirada";
        }

        return "Ativa";
    }

    private static (decimal MonthlyValue, decimal? DevelopmentCost, decimal? RevenueSharePercent) ResolveLicensePricing(
        string salesStrategy,
        ProductPlan plan,
        decimal monthlyValue,
        decimal? developmentCost,
        decimal? revenueSharePercent)
    {
        return salesStrategy switch
        {
            "subscription" => (monthlyValue, null, null),
            "development" => (
                monthlyValue,
                NormalizeOptionalMoney(developmentCost ?? plan.DevelopmentCost)
                    ?? throw BuildPricingValidationException("DevelopmentCost", $"O plano '{plan.Name}' exige custo de desenvolvimento."),
                null),
            "revenue_share" => (
                monthlyValue,
                null,
                NormalizeOptionalPercent(revenueSharePercent ?? plan.RevenueSharePercent)
                    ?? throw BuildPricingValidationException("RevenueSharePercent", $"O plano '{plan.Name}' exige percentual de participação.")),
            _ => throw BuildPricingValidationException("SalesStrategy", $"Estratégia de venda '{salesStrategy}' não suportada.")
        };
    }

    private static ValidationException BuildPricingValidationException(string propertyName, string message) =>
        new([new ValidationFailure(propertyName, message)]);

    private static ProductSummaryDto MapProductSummary(
        Product product,
        IEnumerable<ProductPlan> plans,
        IEnumerable<License> licenses)
    {
        var licensesList = licenses.ToList();
        return new ProductSummaryDto(
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            product.Status,
            product.SalesStrategy,
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            product.CreatedDate.ToIsoDate(),
            product.Version,
            plans
                .OrderBy(x => x.Id)
                .Select(MapPlan)
                .ToArray());
    }

    private static ProductDetailDto MapProductDetail(
        Product product,
        IEnumerable<ProductPlan> plans,
        IEnumerable<License> licenses,
        IEnumerable<Domain.Clients.Client> clients,
        ProductKanbanDto kanban)
    {
        var clientsById = clients.ToDictionary(x => x.Id);
        var licensesList = licenses.ToList();
        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            product.Status,
            product.SalesStrategy,
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            product.CreatedDate.ToIsoDate(),
            product.Version,
            plans
                .OrderBy(x => x.Id)
                .Select(MapPlan)
                .ToArray(),
            licensesList
                .Select(license =>
                {
                    var clientName = clientsById.TryGetValue(license.ClientId, out var client)
                        ? client.Company
                        : "Cliente removido";
                    return MapLicense(license, clientName, product.Name);
                })
                .ToArray(),
            kanban);
    }

    private static ProductPlanDto MapPlan(ProductPlan plan) =>
        new(
            plan.Id,
            plan.Name,
            plan.MaxUsers,
            plan.MonthlyPrice,
            plan.DevelopmentCost,
            plan.MaintenanceCost,
            plan.RevenueSharePercent);

    private static LicenseDto MapLicense(License license, string clientName, string productName) =>
        new(
            license.Id,
            license.ClientId,
            clientName,
            license.ProductId,
            productName,
            license.Plan,
            license.MaxUsers,
            license.ActiveUsers,
            license.Status,
            license.StartDate.ToIsoDate(),
            license.ExpiryDate.ToIsoDate(),
            license.MonthlyValue,
            license.DevelopmentCost,
            license.RevenueSharePercent);

    private static ProductSprintDto MapSprint(ProductSprint sprint) =>
        new(
            sprint.Id,
            sprint.ProductId,
            sprint.Name,
            sprint.StartDate.ToIsoDate(),
            sprint.EndDate.ToIsoDate(),
            sprint.Status);

    private static ProductTaskDto MapTask(ProductTask task) =>
        new(
            task.Id,
            task.ProductId,
            task.SprintId,
            task.Title,
            task.Description,
            task.Column,
            task.Priority,
            task.Assignee,
            DeserializeTags(task.TagsSerialized),
            task.CreatedDate.ToIsoDate());

    private static string SerializeTags(IReadOnlyCollection<string> tags)
    {
        var normalized = tags
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return JsonSerializer.Serialize(normalized);
    }

    private static string[] DeserializeTags(string serializedTags)
    {
        if (string.IsNullOrWhiteSpace(serializedTags))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(serializedTags) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int GetKanbanColumnOrder(string column)
    {
        var index = Array.FindIndex(KanbanColumnOrder, item => string.Equals(item, column, StringComparison.Ordinal));
        return index >= 0 ? index : int.MaxValue;
    }

    private static decimal? NormalizeOptionalMoney(decimal? value) =>
        value.HasValue && value.Value > 0 ? value.Value : null;

    private static decimal? NormalizeOptionalPercent(decimal? value) =>
        value.HasValue && value.Value > 0 ? value.Value : null;

    private ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken) =>
        realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
}
