using System.Net;
using System.Net.Http.Json;
using StokApi.Models;

namespace StokApi.Tests.B2B;

/// <summary>
/// Partner firmanın sisteminin, tedarikçinin Stok API'sini nasıl tüketeceğini simüle eder.
/// Sadece HTTP ile konuşur; controller veya veritabanı bilmez.
/// </summary>
public class PartnerStockClient
{
    private readonly HttpClient _http;

    public PartnerStockClient(HttpClient http)
    {
        _http = http;
    }

    public Task<HttpResponseMessage> CreateProductAsync(string name, int stock, decimal price) =>
        _http.PostAsJsonAsync("/api/products", new { name, stockQuantity = stock, unitPrice = price });

    public Task<HttpResponseMessage> UpdateProductAsync(int id, string name, int stock, decimal price) =>
        _http.PutAsJsonAsync($"/api/products/{id}", new { name, stockQuantity = stock, unitPrice = price });

    public async Task<ProductDto?> GetProductAsync(int id)
    {
        var response = await _http.GetAsync($"/api/products/{id}");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    public Task<HttpResponseMessage> GetRawProductAsync(int id) =>
        _http.GetAsync($"/api/products/{id}");

    /// <summary>Tüm sayfaları dolaşıp ürün kataloğunu çeker (katalog senkronizasyonu).</summary>
    public async Task<List<ProductDto>> GetCatalogAsync(int pageSize = 5)
    {
        var all = new List<ProductDto>();
        var page = 1;
        int totalPages;

        do
        {
            var result = await _http.GetFromJsonAsync<PagedResult<ProductDto>>(
                $"/api/products?page={page}&pageSize={pageSize}");

            all.AddRange(result!.Items);
            totalPages = result.TotalPages;
            page++;
        } while (page <= totalPages);

        return all;
    }
}