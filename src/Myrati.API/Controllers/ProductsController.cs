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

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { productId = response.Id }, response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpPut("{productId}")]
    public async Task<ActionResult<ProductDetailDto>> UpdateProduct(
        string productId,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await productsService.UpdateProductAsync(productId, request, cancellationToken);
        return Ok(response);
    }

    [Authorize(Policy = "BackofficeWrite")]
    [HttpDelete("{productId}")]
    public async Task<IActionResult> DeleteProduct(string productId, CancellationToken cancellationToken)
    {
        await productsService.DeleteProductAsync(productId, cancellationToken);
        return NoContent();
    }
}
