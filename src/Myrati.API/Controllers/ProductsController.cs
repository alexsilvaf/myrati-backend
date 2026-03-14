using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Myrati.Application.Contracts;
using Myrati.Application.Services;

namespace Myrati.API.Controllers;

[ApiController]
[Authorize(Policy = "BackofficeRead")]
[Route("api/v1/backoffice/products")]
public sealed class ProductsController(IProductsService productsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ProductSummaryDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var response = await productsService.GetProductsAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{productId}")]
    public async Task<ActionResult<ProductDetailDto>> GetProduct(string productId, CancellationToken cancellationToken)
    {
        var response = await productsService.GetProductAsync(productId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductCreate")]
    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateProductAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = "ProductCreate")]
    [HttpPost("setup")]
    public async Task<ActionResult<ProductDetailDto>> CreateProductSetup(
        [FromBody] CreateProductSetupRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateProductSetupAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPut("{productId}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(
        string productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateProductAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("{productId}/deployments")]
    public async Task<ActionResult<ProductDetailDto>> RecordProductionDeployment(
        string productId,
        CancellationToken cancellationToken)
    {
        var response = await productsService.RecordProductionDeploymentAsync(productId, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteProduct(string productId, CancellationToken cancellationToken)
    {
        await productsService.DeleteProductAsync(productId, cancellationToken);
        return NoContent();
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPost("{productId}/collaborators")]
    public async Task<ActionResult<ProductCollaboratorDto>> AddCollaborator(
        string productId,
        [FromBody] AddProductCollaboratorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.AddCollaboratorAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpPut("{productId}/collaborators/{memberId}")]
    public async Task<ActionResult<ProductCollaboratorDto>> UpdateCollaborator(
        string productId,
        string memberId,
        [FromBody] UpdateProductCollaboratorRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateCollaboratorAsync(productId, memberId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "ProductScopedWrite")]
    [HttpDelete("{productId}/collaborators/{memberId}")]
    public async Task<IActionResult> DeleteCollaborator(
        string productId,
        string memberId,
        CancellationToken cancellationToken)
    {
        await productsService.DeleteCollaboratorAsync(productId, memberId, cancellationToken);
        return NoContent();
    }
}
