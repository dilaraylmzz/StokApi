using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StokApi.Controllers;
using StokApi.Data;
using StokApi.Models;

namespace StokApi.Tests;

public class ProductsControllerTests
{
    // ---------- Yardımcı metotlar ----------

    private static IMapper CreateMapper() =>
        new ServiceCollection()
            .AddLogging()
            .AddAutoMapper(cfg => cfg.AddMaps(typeof(Program).Assembly))
            .BuildServiceProvider()
            .GetRequiredService<IMapper>();

    private static ProductsController CreateController()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ProductsController(new AppDbContext(options), CreateMapper());
    }

    private static async Task<int> AddProductAsync(
        ProductsController controller, string name, int stock = 1, decimal price = 1)
    {
        var result = await controller.Create(new CreateProductDto
        {
            Name = name, StockQuantity = stock, UnitPrice = price
        });
        return ((ProductDto)((CreatedAtActionResult)result.Result!).Value!).Id;
    }

    // ---------- Mapping ----------

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        CreateMapper().ConfigurationProvider.AssertConfigurationIsValid();
    }

    // ---------- GET /api/Products ----------

    [Fact]
    public async Task GetAll_Pagination_ReturnsCorrectPage()
    {
        var controller = CreateController();
        for (var i = 1; i <= 14; i++)
            await AddProductAsync(controller, $"Urun {i}", stock: i);

        var result = await controller.GetAll(new ProductQueryParameters { Page = 2, PageSize = 10 });

        var paged = result.Value!;
        Assert.Equal(4, paged.Items.Count);
        Assert.Equal(14, paged.TotalCount);
        Assert.Equal(2, paged.TotalPages);
    }

    [Fact]
    public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
    {
        var controller = CreateController();

        var result = await controller.GetAll(new ProductQueryParameters());

        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task GetAll_FilterByPriceRange_ReturnsOnlyMatching()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "Ucuz", price: 1);
        await AddProductAsync(controller, "Orta", price: 5);
        await AddProductAsync(controller, "Pahali", price: 10);

        var result = await controller.GetAll(new ProductQueryParameters { MinPrice = 2, MaxPrice = 6 });

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Orta", item.Name);
    }

    [Fact]
    public async Task GetAll_FilterByStockRange_ReturnsOnlyMatching()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "Az", stock: 10);
        await AddProductAsync(controller, "Orta", stock: 100);
        await AddProductAsync(controller, "Cok", stock: 1000);

        var result = await controller.GetAll(new ProductQueryParameters { MinStock = 50, MaxStock = 500 });

        var item = Assert.Single(result.Value!.Items);
        Assert.Equal("Orta", item.Name);
    }

    [Fact]
    public async Task GetAll_MinPriceGreaterThanMaxPrice_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetAll(new ProductQueryParameters { MinPrice = 10, MaxPrice = 5 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_MinStockGreaterThanMaxStock_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetAll(new ProductQueryParameters { MinStock = 100, MaxStock = 10 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_PageBeyondLastPage_ReturnsEmptyItems()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "Tek");

        var result = await controller.GetAll(new ProductQueryParameters { Page = 5, PageSize = 10 });

        Assert.Empty(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetAll_ReturnsItemsOrderedById()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "C");
        await AddProductAsync(controller, "A");
        await AddProductAsync(controller, "B");

        var result = await controller.GetAll(new ProductQueryParameters());

        var ids = result.Value!.Items.Select(i => i.Id).ToList();
        Assert.Equal(ids.OrderBy(i => i), ids);
    }

    // ---------- GET /api/Products/{id} ----------

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsProduct()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Vida", stock: 7, price: 2.5m);

        var result = await controller.GetById(id);

        var dto = result.Value!;
        Assert.Equal(id, dto.Id);
        Assert.Equal("Vida", dto.Name);
        Assert.Equal(7, dto.StockQuantity);
        Assert.Equal(2.5m, dto.UnitPrice);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // ---------- POST /api/Products ----------

    [Fact]
    public async Task Create_ValidProduct_ReturnsCreated()
    {
        var controller = CreateController();

        var result = await controller.Create(new CreateProductDto
        {
            Name = "  Vida M8  ", StockQuantity = 10, UnitPrice = 1.5m
        });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<ProductDto>(created.Value);
        Assert.Equal("Vida M8", dto.Name);
        Assert.True(dto.Id > 0);
    }

    [Fact]
    public async Task Create_ValidProduct_IsPersisted()
    {
        var controller = CreateController();

        var id = await AddProductAsync(controller, "Kalici");

        var fetched = await controller.GetById(id);
        Assert.Equal("Kalici", fetched.Value!.Name);
    }

    [Fact]
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var controller = CreateController();
        var dto = new CreateProductDto { Name = "Somun", StockQuantity = 1, UnitPrice = 1 };

        await controller.Create(dto);
        var result = await controller.Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_DuplicateName_DoesNotAddSecondRecord()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "Tek");

        await controller.Create(new CreateProductDto { Name = "Tek", StockQuantity = 1, UnitPrice = 1 });

        var all = await controller.GetAll(new ProductQueryParameters());
        Assert.Equal(1, all.Value!.TotalCount);
    }

    [Fact]
    public async Task Create_SameNameWithExtraSpaces_ReturnsConflict()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "Vida");

        var result = await controller.Create(new CreateProductDto
        {
            Name = "   Vida   ", StockQuantity = 1, UnitPrice = 1
        });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // ---------- PUT /api/Products/{id} ----------

    [Fact]
    public async Task Update_ExistingProduct_ReturnsUpdatedProduct()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Eski");

        var result = await controller.Update(id, new UpdateProductDto
        {
            Name = "  Yeni  ", StockQuantity = 50, UnitPrice = 9.5m
        });

        var dto = result.Value!;
        Assert.Equal(id, dto.Id);
        Assert.Equal("Yeni", dto.Name);
        Assert.Equal(50, dto.StockQuantity);
        Assert.Equal(9.5m, dto.UnitPrice);
    }

    [Fact]
    public async Task Update_ExistingProduct_IsPersisted()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Eski");

        await controller.Update(id, new UpdateProductDto { Name = "Yeni", StockQuantity = 99, UnitPrice = 3 });

        var fetched = await controller.GetById(id);
        Assert.Equal("Yeni", fetched.Value!.Name);
        Assert.Equal(99, fetched.Value.StockQuantity);
        Assert.Equal(3m, fetched.Value.UnitPrice);
    }

    [Fact]
    public async Task Update_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Update(99, new UpdateProductDto { Name = "X", StockQuantity = 1, UnitPrice = 1 });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_NameUsedByAnotherProduct_ReturnsConflict()
    {
        var controller = CreateController();
        await AddProductAsync(controller, "A");
        var secondId = await AddProductAsync(controller, "B");

        var result = await controller.Update(secondId, new UpdateProductDto { Name = "A", StockQuantity = 1, UnitPrice = 1 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_SameNameOnSameProduct_IsAllowed()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Ayni");

        var result = await controller.Update(id, new UpdateProductDto { Name = "Ayni", StockQuantity = 5, UnitPrice = 1 });

        Assert.Equal(5, result.Value!.StockQuantity);
    }

    // ---------- DELETE /api/Products/{id} ----------

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Pul");

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.Delete(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ThenGetById_ReturnsNotFound()
    {
        var controller = CreateController();
        var id = await AddProductAsync(controller, "Silinecek");

        await controller.Delete(id);

        var result = await controller.GetById(id);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}