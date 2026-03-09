using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface ILicenseActivationService
{
    Task<LicenseActivationResponse> ActivateAsync(
        LicenseActivationRequest request,
        CancellationToken cancellationToken = default);
}
