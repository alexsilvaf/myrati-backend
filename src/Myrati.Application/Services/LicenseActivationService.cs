using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Myrati.Application.Abstractions;
using Myrati.Application.Common;
using Myrati.Application.Common.Exceptions;
using Myrati.Application.Contracts;
using Myrati.Domain.Products;

namespace Myrati.Application.Services;

public sealed class LicenseActivationService(
    IMyratiDbContext dbContext,
    IValidator<LicenseActivationRequest> activationValidator) : ILicenseActivationService
{
    public async Task<LicenseActivationResponse> ActivateAsync(
        LicenseActivationRequest request,
        CancellationToken cancellationToken = default)
    {
        await activationValidator.ValidateRequestAsync(request, cancellationToken);

        var normalizedProductId = request.ProductId.Trim().ToLowerInvariant();
        var normalizedLicenseKey = request.LicenseKey.Trim().ToUpperInvariant();

        var product = await dbContext.Products
            .FirstOrDefaultAsync(x => x.Id.ToLower() == normalizedProductId, cancellationToken)
            ?? throw new EntityNotFoundException("Produto", request.ProductId);

        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(x => x.Id.ToUpper() == normalizedLicenseKey, cancellationToken)
            ?? throw new EntityNotFoundException("Licença", request.LicenseKey);

        if (!string.Equals(license.ProductId, product.Id, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("A licença informada não pertence ao produto solicitado.");
        }

        if (product.Status != "Ativo")
        {
            throw new ConflictException("O produto informado não está disponível para ativação.");
        }

        var client = await dbContext.Clients
            .FirstOrDefaultAsync(x => x.Id == license.ClientId, cancellationToken)
            ?? throw new EntityNotFoundException("Cliente", license.ClientId);

        if (client.Status != "Ativo")
        {
            throw new ConflictException("A licença está vinculada a um cliente inativo.");
        }

        var effectiveStatus = await ResolveEffectiveLicenseStatusAsync(license, cancellationToken);
        if (effectiveStatus != "Ativa")
        {
            throw new ConflictException(GetBlockedActivationMessage(effectiveStatus));
        }

        return new LicenseActivationResponse(
            license.Id,
            product.Id,
            product.Name,
            client.Id,
            client.Company,
            license.Plan,
            effectiveStatus,
            license.StartDate.ToIsoDate(),
            license.ExpiryDate.ToIsoDate(),
            license.MaxUsers,
            license.ActiveUsers,
            "Ativação autorizada para o produto informado.");
    }

    private async Task<string> ResolveEffectiveLicenseStatusAsync(License license, CancellationToken cancellationToken)
    {
        if (license.Status == "Suspensa")
        {
            return license.Status;
        }

        var effectiveStatus = DetermineStatus(license.StartDate, license.ExpiryDate);
        if (license.Status != effectiveStatus)
        {
            license.Status = effectiveStatus;
            dbContext.Update(license);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return effectiveStatus;
    }

    private static string DetermineStatus(DateOnly startDate, DateOnly expiryDate)
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

    private static string GetBlockedActivationMessage(string status) =>
        status switch
        {
            "Suspensa" => "A licença está suspensa e não pode ser ativada.",
            "Expirada" => "A licença expirou e não pode mais ser ativada.",
            "Pendente" => "A licença ainda não está válida para ativação.",
            _ => $"A licença não pode ser ativada porque está em status '{status}'."
        };
}
