using System.Net;
using System.Net.Http.Json;
using StokApi.Models;

namespace StokApi.Tests;

public class ProductsApiTests : IClassFixture<StokApiFactory>
{
    private readonly HttpClient _client;

    public ProductsApiTests(StokApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    [Fact]
    public async Task Post_ValidProduct_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { name = UniqueName("Vida"), stockQuantity = 10, unitPrice = 1.25 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        Assert.True(product!.Id > 0);
    }

    [Theory]
    [InlineData("", 10, 5)]
    [InlineData("Somun", -5, 5)]
    [InlineData("Somun", 10, -1)]
    public async Task Post_InvalidData_Returns400(string name, int stock, decimal price)
    {
        var response = await _client.PostAsJsonAsync("/api/products",
            new { name, stockQuantity = stock, unitPrice = price });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateName_Returns409()
    {
        var body = new { name = UniqueName("Pul"), stockQuantity = 1, unitPrice = 1 };

        await _client.PostAsJsonAsync("/api/products", body);
        var response = await _client.PostAsJsonAsync("/api/products", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/products/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_InvalidPage_Returns400()
    {
        var response = await _client.GetAsync("/api/products?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_Create_Get_Update_Delete()
    {
        var create = await _client.PostAsJsonAsync("/api/products",
            new { name = UniqueName("Flow"), stockQuantity = 5, unitPrice = 2 });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ProductDto>();

        var get = await _client.GetAsync($"/api/products/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var newName = UniqueName("Flow2");
        var put = await _client.PutAsJsonAsync($"/api/products/{created.Id}",
            new { name = newName, stockQuantity = 50, unitPrice = 3 });
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var updated = await put.Content.ReadFromJsonAsync<ProductDto>();
        Assert.Equal(newName, updated!.Name);
        Assert.Equal(50, updated.StockQuantity);

        var delete = await _client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await _client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, after.StatusCode);
    }
    [Theory]
    [InlineData(100, HttpStatusCode.Created)]
    [InlineData(101, HttpStatusCode.BadRequest)]
    public async Task Post_NameLength_Boundary(int length, HttpStatusCode expected)
    {
        var name = new string('x', length - 36) + Guid.NewGuid().ToString();   // 36 karakter benzersiz kısım
        if (length < 37) name = new string('x', length);

        var response = await _client.PostAsJsonAsync("/api/products",
            new { name, stockQuantity = 1, unitPrice = 1 });

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(100, HttpStatusCode.OK)]
    [InlineData(101, HttpStatusCode.BadRequest)]
    public async Task Get_PageSize_Boundary(int pageSize, HttpStatusCode expected)
    {
        var response = await _client.GetAsync($"/api/products?pageSize={pageSize}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Put_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/api/products/999999",
            new { name = "Yok", stockQuantity = 1, unitPrice = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}