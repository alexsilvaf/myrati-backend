namespace Myrati.Application.Contracts;

public sealed record UserDirectoryQuery(string? Search, string? Status, string? ProductId);

public sealed record UserDirectoryItemDto(
    string Id,
    string Name,
    string Email,
    string ClientId,
    string ClientName,
    string ProductId,
    string ProductName,
    string LastActive,
    string Status);
