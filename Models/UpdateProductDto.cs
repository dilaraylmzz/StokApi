using System.ComponentModel.DataAnnotations;

namespace StokApi.Models;

public class UpdateProductDto
{
    [Required(ErrorMessage = "Ürün adı boş olamaz.")]
    [StringLength(100, ErrorMessage = "Ürün adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0'dan küçük olamaz.")]
    public int StockQuantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Birim fiyat 0'dan küçük olamaz.")]
    public decimal UnitPrice { get; set; }
}