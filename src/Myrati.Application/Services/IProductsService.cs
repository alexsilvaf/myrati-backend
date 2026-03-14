using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IProductsService
{
    Task<IReadOnlyCollection<ProductSummaryDto>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductAsync(string productId, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductSetupAsync(CreateProductSetupRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(string productId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> RecordProductionDeploymentAsync(string productId, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(string productId, CancellationToken cancellationToken = default);
    Task<LicenseDto> CreateLicenseAsync(string productId, CreateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<LicenseDto> UpdateLicenseAsync(string licenseId, UpdateLicenseRequest request, CancellationToken cancellationToken = default);
    Task<LicenseDto> SuspendLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
    Task<LicenseDto> ReactivateLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
    Task DeleteLicenseAsync(string licenseId, CancellationToken cancellationToken = default);
    Task<ProductKanbanDto> GetKanbanAsync(string productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ProductExpenseDto>> GetExpensesAsync(string productId, CancellationToken cancellationToken = default);
    Task<ProductBacklogImportResultDto> ImportBacklogAsync(string productId, ImportProductBacklogRequest request, CancellationToken cancellationToken = default);
    Task<ProductSprintDto> CreateSprintAsync(string productId, CreateProductSprintRequest request, CancellationToken cancellationToken = default);
    Task<ProductSprintDto> UpdateSprintAsync(string productId, string sprintId, UpdateProductSprintRequest request, CancellationToken cancellationToken = default);
    Task DeleteSprintAsync(string productId, string sprintId, CancellationToken cancellationToken = default);
    Task<ProductTaskDto> CreateTaskAsync(string productId, CreateProductTaskRequest request, CancellationToken cancellationToken = default);
    Task<ProductTaskDto> UpdateTaskAsync(string productId, string taskId, UpdateProductTaskRequest request, CancellationToken cancellationToken = default);
    Task DeleteTaskAsync(string productId, string taskId, CancellationToken cancellationToken = default);
    Task<ProductExpenseDto> CreateExpenseAsync(string productId, CreateProductExpenseRequest request, CancellationToken cancellationToken = default);
    Task<ProductExpenseDto> UpdateExpenseAsync(string productId, string expenseId, UpdateProductExpenseRequest request, CancellationToken cancellationToken = default);
    Task DeleteExpenseAsync(string productId, string expenseId, CancellationToken cancellationToken = default);
    Task<ProductCollaboratorDto> AddCollaboratorAsync(string productId, AddProductCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task<ProductCollaboratorDto> UpdateCollaboratorAsync(string productId, string memberId, UpdateProductCollaboratorRequest request, CancellationToken cancellationToken = default);
    Task DeleteCollaboratorAsync(string productId, string memberId, CancellationToken cancellationToken = default);
}
