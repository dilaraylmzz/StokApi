using System.ComponentModel.DataAnnotations;
using StokApi.Models;

namespace StokApi.Tests;

public class ValidationBoundaryTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    // Ürün adı uzunluğu: 100 kabul, 101 ret, boş ret
    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(0, false)]
    public void ProductName_Length_Boundary(int length, bool expectedValid)
    {
        var dto = new CreateProductDto { Name = new string('a', length), StockQuantity = 1, UnitPrice = 1 };

        Assert.Equal(expectedValid, Validate(dto).Count == 0);
    }

    // Stok: 0 kabul, -1 ret
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public void StockQuantity_Boundary(int stock, bool expectedValid)
    {
        var dto = new CreateProductDto { Name = "Urun", StockQuantity = stock, UnitPrice = 1 };

        Assert.Equal(expectedValid, Validate(dto).Count == 0);
    }

    // Fiyat: 0 kabul, -0.01 ret
    [Theory]
    [InlineData("0", true)]
    [InlineData("0.01", true)]
    [InlineData("-0.01", false)]
    public void UnitPrice_Boundary(string price, bool expectedValid)
    {
        var dto = new CreateProductDto
        {
            Name = "Urun", StockQuantity = 1,
            UnitPrice = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)
        };

        Assert.Equal(expectedValid, Validate(dto).Count == 0);
    }

    // Sayfa: 0 ret, 1 kabul
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void Page_Boundary(int page, bool expectedValid)
    {
        var query = new ProductQueryParameters { Page = page };

        Assert.Equal(expectedValid, Validate(query).Count == 0);
    }

    // Sayfa boyutu: 0 ret, 1 kabul, 100 kabul, 101 ret
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public void PageSize_Boundary(int pageSize, bool expectedValid)
    {
        var query = new ProductQueryParameters { PageSize = pageSize };

        Assert.Equal(expectedValid, Validate(query).Count == 0);
    }

    // UpdateProductDto de aynı kuralları taşımalı
    [Theory]
    [InlineData(101, false)]
    [InlineData(100, true)]
    public void UpdateDto_NameLength_Boundary(int length, bool expectedValid)
    {
        var dto = new UpdateProductDto { Name = new string('a', length), StockQuantity = 1, UnitPrice = 1 };

        Assert.Equal(expectedValid, Validate(dto).Count == 0);
    }
}