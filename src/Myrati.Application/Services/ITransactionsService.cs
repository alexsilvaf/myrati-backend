using Myrati.Application.Contracts;

namespace Myrati.Application.Services;

public interface ITransactionsService
{
    Task<IReadOnlyCollection<CashTransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<CashTransactionDto> CreateTransactionAsync(CreateCashTransactionRequest request, CancellationToken cancellationToken = default);
    Task<CashTransactionDto> UpdateTransactionAsync(string transactionId, UpdateCashTransactionRequest request, CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(string transactionId, CancellationToken cancellationToken = default);
}
