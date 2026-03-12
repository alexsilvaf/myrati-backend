namespace Myrati.Application.Contracts;

public sealed record PortalMeDto(
    string Id,
    string Name,
    string Email,
    string Company,
    IReadOnlyCollection<LicenseDto> Licenses);
