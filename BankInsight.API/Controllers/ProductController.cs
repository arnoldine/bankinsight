using System.Threading.Tasks;
using BankInsight.API.DTOs;
using BankInsight.API.Infrastructure;
using BankInsight.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankInsight.API.Controllers;

[Authorize]
[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [RequirePermission("VIEW_PRODUCTS")]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _productService.GetProductListItemsAsync();
        return Ok(products);
    }

    [HttpPost]
    [RequirePermission("MANAGE_PRODUCTS")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        var product = await _productService.CreateProductAsync(request);
        return StatusCode(201, product);
    }

    [HttpPut("{id}")]
    [RequirePermission("MANAGE_PRODUCTS")]
    public async Task<IActionResult> UpdateProduct(string id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.UpdateProductAsync(id, request);
        if (product == null) return NotFound(new { message = "Product not found" });
        return Ok(product);
    }

    [HttpPut("{id}/lifecycle")]
    [RequirePermission("MANAGE_PRODUCTS")]
    public async Task<IActionResult> UpdateLifecycle(string id, [FromBody] ProductLifecycleUpdateRequest request)
    {
        var product = await _productService.UpdateLifecycleAsync(id, request);
        if (product == null) return NotFound(new { message = "Product not found" });
        return Ok(product);
    }

    [HttpPost("{id}/simulate")]
    [RequirePermission("VIEW_PRODUCTS")]
    public async Task<IActionResult> SimulateProduct(string id, [FromBody] ProductSimulationRequest request)
    {
        var result = await _productService.SimulateProductAsync(id, request);
        if (result == null) return NotFound(new { message = "Product not found" });
        return Ok(result);
    }
}
