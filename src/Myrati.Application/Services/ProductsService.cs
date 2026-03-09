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
    IRealtimeEventPublisher realtimeEventPublisher) : IProductsService
{
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
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", productId);

        var plans = await dbContext.ProductPlans
            .Where(x => x.ProductId == product.Id)
            .OrderBy(x => x.MonthlyPrice)
            .ToListAsync(cancellationToken);
        var licenses = await dbContext.Licenses
            .Where(x => x.ProductId == product.Id)
            .OrderByDescending(x => x.Status == "Ativa")
            .ThenBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);
        var clients = await dbContext.Clients.ToListAsync(cancellationToken);

        return MapProductDetail(product, plans, licenses, clients);
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
            Version = request.Version.Trim(),
            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await dbContext.AddAsync(product, cancellationToken);

        var planIndex = 1;
        foreach (var plan in request.Plans)
        {
            await dbContext.AddAsync(new ProductPlan
            {
                Id = IdGenerator.CreatePlanId(productId, planIndex++),
                ProductId = productId,
                Name = plan.Name.Trim(),
                MaxUsers = plan.MaxUsers,
                MonthlyPrice = plan.MonthlyPrice
            }, cancellationToken);
        }

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

        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", productId);

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
        product.Version = request.Version.Trim();
        dbContext.Update(product);

        var existingPlans = await dbContext.ProductPlans
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

        foreach (var plan in existingPlans)
        {
            dbContext.Remove(plan);
        }

        var planIndex = 1;
        foreach (var plan in request.Plans)
        {
            await dbContext.AddAsync(new ProductPlan
            {
                Id = IdGenerator.CreatePlanId(productId, planIndex++),
                ProductId = productId,
                Name = plan.Name.Trim(),
                MaxUsers = plan.MaxUsers,
                MonthlyPrice = plan.MonthlyPrice
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var response = await GetProductAsync(productId, cancellationToken);
        await PublishBackofficeEventAsync("product.updated", response, cancellationToken);
        return response;
    }

    public async Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", productId);

        var hasLicenses = await dbContext.Licenses.AnyAsync(x => x.ProductId == productId, cancellationToken);
        if (hasLicenses)
        {
            throw new ConflictException("Não é possível remover um produto que ainda possui licenças vinculadas.");
        }

        var plans = await dbContext.ProductPlans
            .Where(x => x.ProductId == productId)
            .ToListAsync(cancellationToken);

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

        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", productId);
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == request.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", request.ClientId);

        if (client.Status != "Ativo")
        {
            throw new ConflictException("A licença só pode ser vinculada a clientes ativos.");
        }

        var plan = await GetPlanAsync(productId, request.Plan, cancellationToken);
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
            MonthlyValue = request.MonthlyValue
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
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == license.ProductId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", license.ProductId);
        var plan = await GetPlanAsync(product.Id, request.Plan, cancellationToken);

        var startDate = RequestValidation.ParseIsoDate(request.StartDate, nameof(request.StartDate));
        var expiryDate = RequestValidation.ParseIsoDate(request.ExpiryDate, nameof(request.ExpiryDate));
        ValidateLicenseDates(startDate, expiryDate);

        license.ClientId = client.Id;
        license.Plan = plan.Name;
        license.MaxUsers = plan.MaxUsers;
        license.StartDate = startDate;
        license.ExpiryDate = expiryDate;
        license.MonthlyValue = request.MonthlyValue;
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

    private async Task<LicenseDto> GetLicenseAsync(string licenseId, CancellationToken cancellationToken)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id == licenseId, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", licenseId);
        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == license.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", license.ClientId);
        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id == license.ProductId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", license.ProductId);

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

    private static void ValidateLicenseDates(DateOnly startDate, DateOnly expiryDate)
    {
        if (expiryDate <= startDate)
        {
            throw new ValidationException(
                [new ValidationFailure("ExpiryDate", "A data de expiração deve ser maior que a data inicial.")]);
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
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            product.CreatedDate.ToIsoDate(),
            product.Version,
            plans
                .OrderBy(x => x.MonthlyPrice)
                .Select(MapPlan)
                .ToArray());
    }

    private static ProductDetailDto MapProductDetail(
        Product product,
        IEnumerable<ProductPlan> plans,
        IEnumerable<License> licenses,
        IEnumerable<Domain.Clients.Client> clients)
    {
        var clientsById = clients.ToDictionary(x => x.Id);
        var licensesList = licenses.ToList();
        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Description,
            product.Category,
            product.Status,
            licensesList.Count,
            licensesList.Count(x => x.Status == "Ativa"),
            licensesList.Where(x => x.Status == "Ativa").Sum(x => x.MonthlyValue),
            product.CreatedDate.ToIsoDate(),
            product.Version,
            plans
                .OrderBy(x => x.MonthlyPrice)
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
                .ToArray());
    }

    private static ProductPlanDto MapPlan(ProductPlan plan) =>
        new(plan.Id, plan.Name, plan.MaxUsers, plan.MonthlyPrice);

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
            license.MonthlyValue);

    private ValueTask PublishBackofficeEventAsync(string eventType, object payload, CancellationToken cancellationToken) =>
        realtimeEventPublisher.PublishAsync(
            new RealtimeEvent(RealtimeChannels.Backoffice, eventType, DateTimeOffset.UtcNow, payload),
            cancellationToken);
}
