using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Domain.Entities;
using StemCellsPro.Shared.Responses;

namespace StemCellsPro.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Requires valid token
public class ProductsController : ControllerBase
{
    private readonly IGenericRepository<Product> _productRepository;

    public ProductsController(IGenericRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productRepository.GetAllAsync();
        return Ok(new ApiResponse<IReadOnlyList<Product>>(products, "Products retrieved successfully."));
    }

}
