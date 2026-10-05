using System.ComponentModel.DataAnnotations;

namespace StokApi.Models;

public class ProductQueryParameters
{
    public string? Name { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MinPrice 0'dan küçük olamaz.")]
    public decimal? MinPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "MaxPrice 0'dan küçük olamaz.")]
    public decimal? MaxPrice { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "MinStock 0'dan küçük olamaz.")]
    public int? MinStock { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "MaxStock 0'dan küçük olamaz.")]
    public int? MaxStock { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Page 1 veya daha büyük olmalı.")]
    public int Page { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize 1 ile 100 arasında olmalı.")]
    public int PageSize { get; set; } = 10;
}