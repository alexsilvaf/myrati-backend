using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/transactions")]
public sealed class TransactionsController(ITransactionsService transactionsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CashTransactionDto>>> GetTransactions(CancellationToken cancellationToken)
    {
        var response = await transactionsService.GetTransactionsAsync(cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost]
    public async Task<ActionResult<CashTransactionDto>> CreateTransaction(
        [FromBody] CreateCashTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await transactionsService.CreateTransactionAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("{transactionId}")]
    public async Task<ActionResult<CashTransactionDto>> UpdateTransaction(
        string transactionId,
        [FromBody] UpdateCashTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await transactionsService.UpdateTransactionAsync(transactionId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("{transactionId}")]
    public async Task<IActionResult> DeleteTransaction(string transactionId, CancellationToken cancellationToken)
    {
        await transactionsService.DeleteTransactionAsync(transactionId, cancellationToken);
        return NoContent();
    }
}
