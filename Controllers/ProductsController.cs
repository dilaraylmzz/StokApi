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

    // GET /api/products?name=vida&minPrice=1&maxPrice=5&page=1&pageSize=10
    [HttpGet]
    public async Task<ActionResult<PagedResult<Product>>> GetAll([FromQuery] ProductQueryParameters query)
    {
        if (query.MinPrice > query.MaxPrice)
            return BadRequest("MinPrice, MaxPrice'tan büyük olamaz.");
        if (query.MinStock > query.MaxStock)
            return BadRequest("MinStock, MaxStock'tan büyük olamaz.");

        var products = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Name))
            products = products.Where(p => EF.Functions.ILike(p.Name, $"%{query.Name}%"));
        if (query.MinPrice.HasValue)
            products = products.Where(p => p.UnitPrice >= query.MinPrice.Value);
        if (query.MaxPrice.HasValue)
            products = products.Where(p => p.UnitPrice <= query.MaxPrice.Value);
        if (query.MinStock.HasValue)
            products = products.Where(p => p.StockQuantity >= query.MinStock.Value);
        if (query.MaxStock.HasValue)
            products = products.Where(p => p.StockQuantity <= query.MaxStock.Value);

        var totalCount = await products.CountAsync();

        var items = await products
            .OrderBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
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
        var name = dto.Name.Trim();

        if (await _context.Products.AnyAsync(p => p.Name == name))
            return Conflict($"'{name}' adlı ürün zaten mevcut.");

        var product = new Product
        {
            Name = name,
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

        var name = dto.Name.Trim();

        if (await _context.Products.AnyAsync(p => p.Name == name && p.Id != id))
            return Conflict($"'{name}' adlı başka bir ürün zaten mevcut.");

        product.Name = name;
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