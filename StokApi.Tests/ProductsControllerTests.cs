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

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        CreateMapper().ConfigurationProvider.AssertConfigurationIsValid();
    }

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
    public async Task Create_DuplicateName_ReturnsConflict()
    {
        var controller = CreateController();
        var dto = new CreateProductDto { Name = "Somun", StockQuantity = 1, UnitPrice = 1 };

        await controller.Create(dto);
        var result = await controller.Create(dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.GetById(99);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var controller = CreateController();
        var created = await controller.Create(new CreateProductDto { Name = "Pul", StockQuantity = 1, UnitPrice = 1 });
        var id = ((ProductDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetAll_Pagination_ReturnsCorrectPage()
    {
        var controller = CreateController();
        for (var i = 1; i <= 14; i++)
            await controller.Create(new CreateProductDto { Name = $"Urun {i}", StockQuantity = i, UnitPrice = 1 });

        var result = await controller.GetAll(new ProductQueryParameters { Page = 2, PageSize = 10 });

        var paged = result.Value!;
        Assert.Equal(4, paged.Items.Count);
        Assert.Equal(14, paged.TotalCount);
        Assert.Equal(2, paged.TotalPages);
    }
    
        // ---------- Update testleri ----------

    [Fact]
    public async Task Update_ExistingProduct_ReturnsUpdatedProduct()
    {
        var controller = CreateController();
        var created = await controller.Create(new CreateProductDto { Name = "Eski", StockQuantity = 1, UnitPrice = 1 });
        var id = ((ProductDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await controller.Update(id, new UpdateProductDto
        {
            Name = "  Yeni  ", StockQuantity = 50, UnitPrice = 9.5m
        });

        var dto = result.Value!;
        Assert.Equal(id, dto.Id);          // Id değişmedi
        Assert.Equal("Yeni", dto.Name);    // Trim çalıştı
        Assert.Equal(50, dto.StockQuantity);
        Assert.Equal(9.5m, dto.UnitPrice);
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
        await controller.Create(new CreateProductDto { Name = "A", StockQuantity = 1, UnitPrice = 1 });
        var second = await controller.Create(new CreateProductDto { Name = "B", StockQuantity = 1, UnitPrice = 1 });
        var secondId = ((ProductDto)((CreatedAtActionResult)second.Result!).Value!).Id;

        var result = await controller.Update(secondId, new UpdateProductDto { Name = "A", StockQuantity = 1, UnitPrice = 1 });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task Update_SameNameOnSameProduct_IsAllowed()
    {
        var controller = CreateController();
        var created = await controller.Create(new CreateProductDto { Name = "Ayni", StockQuantity = 1, UnitPrice = 1 });
        var id = ((ProductDto)((CreatedAtActionResult)created.Result!).Value!).Id;

        var result = await controller.Update(id, new UpdateProductDto { Name = "Ayni", StockQuantity = 5, UnitPrice = 1 });

        Assert.Equal(5, result.Value!.StockQuantity);
    }

    // ---------- Sınır değer testleri (min/max kontrolü) ----------

    [Fact]
    public async Task GetAll_MinPriceGreaterThanMaxPrice_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetAll(new ProductQueryParameters { MinPrice = 10, MaxPrice = 5 });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAll_MinPriceEqualsMaxPrice_IsAllowed()
    {
        var controller = CreateController();
        await controller.Create(new CreateProductDto { Name = "Tam", StockQuantity = 1, UnitPrice = 5 });

        var result = await controller.GetAll(new ProductQueryParameters { MinPrice = 5, MaxPrice = 5 });

        Assert.Single(result.Value!.Items);   // sınır dahil
    }

    [Fact]
    public async Task GetAll_PageBeyondLastPage_ReturnsEmptyItems()
    {
        var controller = CreateController();
        await controller.Create(new CreateProductDto { Name = "Tek", StockQuantity = 1, UnitPrice = 1 });

        var result = await controller.GetAll(new ProductQueryParameters { Page = 5, PageSize = 10 });

        Assert.Empty(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
    }
}