namespace Myrati.Application.Contracts;

public sealed record CashTransactionDto(
    string Id,
    string Type,
    string Category,
    decimal Amount,
    string Description,
    string? ReferenceProductId,
    string? ReferenceProductName,
    string Date,
    decimal BalanceAfter);

public sealed record CreateCashTransactionRequest(
    string Type,
    string Category,
    decimal Amount,
    string Description,
    string? ReferenceProductId,
    string Date);

public sealed record UpdateCashTransactionRequest(
    string Type,
    string Category,
    decimal Amount,
    string Description,
    string? ReferenceProductId,
    string Date);
