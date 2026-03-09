using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface IPublicSiteService
{
    Task<ContactResponse> SubmitContactAsync(ContactRequest request, CancellationToken cancellationToken = default);
    Task<SystemStatusResponse> GetSystemStatusAsync(CancellationToken cancellationToken = default);
}
