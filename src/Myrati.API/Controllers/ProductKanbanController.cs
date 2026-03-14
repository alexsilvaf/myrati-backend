using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Route("api/v1/backoffice/products/{productId}")]
public sealed class ProductKanbanController(IProductsService productsService) : ControllerBase
{
    [Authorize(Policy = "BackofficeRead")]
    [HttpGet("kanban")]
    public async Task<ActionResult<ProductKanbanDto>> GetKanban(string productId, CancellationToken cancellationToken)
    {
        var response = await productsService.GetKanbanAsync(productId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeRead")]
    [HttpGet("expenses")]
    public async Task<ActionResult<IReadOnlyCollection<ProductExpenseDto>>> GetExpenses(
        string productId,
        CancellationToken cancellationToken)
    {
        var response = await productsService.GetExpensesAsync(productId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("backlog/import")]
    public async Task<ActionResult<ProductBacklogImportResultDto>> ImportBacklog(
        string productId,
        [FromBody] ImportProductBacklogRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.ImportBacklogAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("sprints")]
    public async Task<ActionResult<ProductSprintDto>> CreateSprint(
        string productId,
        [FromBody] CreateProductSprintRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateSprintAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPut("sprints/{sprintId}")]
    public async Task<ActionResult<ProductSprintDto>> UpdateSprint(
        string productId,
        string sprintId,
        [FromBody] UpdateProductSprintRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateSprintAsync(productId, sprintId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpDelete("sprints/{sprintId}")]
    public async Task<IActionResult> DeleteSprint(string productId, string sprintId, CancellationToken cancellationToken)
    {
        await productsService.DeleteSprintAsync(productId, sprintId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("tasks")]
    public async Task<ActionResult<ProductTaskDto>> CreateTask(
        string productId,
        [FromBody] CreateProductTaskRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateTaskAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPut("tasks/{taskId}")]
    public async Task<ActionResult<ProductTaskDto>> UpdateTask(
        string productId,
        string taskId,
        [FromBody] UpdateProductTaskRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateTaskAsync(productId, taskId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpDelete("tasks/{taskId}")]
    public async Task<IActionResult> DeleteTask(string productId, string taskId, CancellationToken cancellationToken)
    {
        await productsService.DeleteTaskAsync(productId, taskId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("expenses")]
    public async Task<ActionResult<ProductExpenseDto>> CreateExpense(
        string productId,
        [FromBody] CreateProductExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateExpenseAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPut("expenses/{expenseId}")]
    public async Task<ActionResult<ProductExpenseDto>> UpdateExpense(
        string productId,
        string expenseId,
        [FromBody] UpdateProductExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateExpenseAsync(productId, expenseId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpDelete("expenses/{expenseId}")]
    public async Task<IActionResult> DeleteExpense(string productId, string expenseId, CancellationToken cancellationToken)
    {
        await productsService.DeleteExpenseAsync(productId, expenseId, cancellationToken);
        return NoContent();
    }
}
