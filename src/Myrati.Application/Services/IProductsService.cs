using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IProductsService
{
    Task<IReadOnlyCollection<ProductSummaryDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductAsync(string productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default);
    Task<LicenseDto> CreateLicenseAsync(string productId, CreateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<LicenseDto> UpdateLicenseAsync(string licenseId, UpdateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<LicenseDto> SuspendLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
    Task<LicenseDto> ReactivateLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
    Task DeleteLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
}
