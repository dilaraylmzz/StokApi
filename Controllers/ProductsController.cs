using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StokApi.Data;
using StokApi.Models;

namespace StokApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/products
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll()
    {
        return await _context.Products.ToListAsync();
    }

    // GET /api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound($"{id} numaralı ürün bulunamadı.");

        return product;
    }

    // POST /api/products
    [HttpPost]
    public async Task<ActionResult<Product>> Create(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name.Trim(),
            StockQuantity = dto.StockQuantity,
            UnitPrice = dto.UnitPrice
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT /api/products/5
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> Update(int id, CreateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound($"{id} numaralı ürün bulunamadı.");

        product.Name = dto.Name.Trim();
        product.StockQuantity = dto.StockQuantity;
        product.UnitPrice = dto.UnitPrice;

        await _context.SaveChangesAsync();

        return Ok(product);
    }

    // DELETE /api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound($"{id} numaralı ürün bulunamadı.");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}