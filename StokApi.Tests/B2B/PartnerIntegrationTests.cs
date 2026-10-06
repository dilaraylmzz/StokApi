using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace StokApi.Tests.B2B;

public class PartnerIntegrationTests : IClassFixture<StokApiFactory>
{
    private readonly PartnerStockClient _partner;

    public PartnerIntegrationTests(StokApiFactory factory)
    {
        _partner = new PartnerStockClient(factory.CreateClient());
    }

    private static string UniqueName(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    // 1) Sözleşme (contract) testi: partnerin güvendiği JSON yapısı değişmemeli
    [Fact]
    public async Task Contract_ProductResponse_HasAgreedFields()
    {
        var create = await _partner.CreateProductAsync(UniqueName("Kontrat"), 10, 2.5m);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetInt32();

        var response = await _partner.GetRawProductAsync(id);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var fields = json.EnumerateObject().Select(p => p.Name).OrderBy(n => n).ToArray();
        Assert.Equal(new[] { "id", "name", "stockQuantity", "unitPrice" }, fields);
        Assert.Equal(JsonValueKind.Number, json.GetProperty("stockQuantity").ValueKind);
        Assert.Equal(JsonValueKind.Number, json.GetProperty("unitPrice").ValueKind);
    }

    // 2) Katalog senkronizasyonu: partner tüm sayfaları dolaşıp bütün ürünleri alabilmeli
    [Fact]
    public async Task CatalogSync_PartnerReadsAllPages()
    {
        var prefix = UniqueName("Katalog");
        var createdNames = new List<string>();
        for (var i = 1; i <= 12; i++)
        {
            var name = $"{prefix}-{i}";
            var response = await _partner.CreateProductAsync(name, i, 1);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            createdNames.Add(name);
        }

        var catalog = await _partner.GetCatalogAsync(pageSize: 5);

        var catalogNames = catalog.Select(p => p.Name).ToHashSet();
        Assert.All(createdNames, n => Assert.Contains(n, catalogNames));
        Assert.Equal(catalog.Count, catalog.Select(p => p.Id).Distinct().Count()); // tekrar eden kayıt yok
    }

    // 3) Tedarikçi stoğu güncelleyince partner güncel değeri görmeli
    [Fact]
    public async Task StockUpdate_IsVisibleToPartner()
    {
        var create = await _partner.CreateProductAsync(UniqueName("Stok"), 100, 5);
        var created = await create.Content.ReadFromJsonAsync<StokApi.Models.ProductDto>();

        var update = await _partner.UpdateProductAsync(created!.Id, created.Name, 40, 5);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var seenByPartner = await _partner.GetProductAsync(created.Id);
        Assert.Equal(40, seenByPartner!.StockQuantity);
    }

    // 4) Hata durumları: partner sistemi hataları doğru yorumlayabilmeli
    [Fact]
    public async Task UnknownProduct_PartnerGetsNull()
    {
        var product = await _partner.GetProductAsync(999999);

        Assert.Null(product);
    }

    [Fact]
    public async Task InvalidData_ReturnsProblemJson400()
    {
        var response = await _partner.CreateProductAsync("", -1, -1);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.GetProperty("errors").EnumerateObject().Any());
    }

    [Fact]
    public async Task DuplicateProduct_ReturnsConflict409()
    {
        var name = UniqueName("Cift");

        await _partner.CreateProductAsync(name, 1, 1);
        var second = await _partner.CreateProductAsync(name, 1, 1);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}