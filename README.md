# StokApi

.NET 8 ile yazılmış stok yönetimi Web API projesi.

## Teknolojiler
.NET 8, ASP.NET Core, Entity Framework Core, PostgreSQL, AutoMapper, Swagger, xUnit

## Gereksinimler
- .NET 8 SDK
- PostgreSQL (17 ile denendi)

## Kurulum

1. Repoyu klonla.
2. `appsettings.json` içine bağlantı cümlesini ekle (parolanı kendi PostgreSQL parolanla değiştir):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=stokdb;Username=postgres;Password=PAROLAN"
  }
}
```

3. Çalıştır:

```
dotnet run
```

Uygulama açılırken `stokdb` veritabanını ve tabloyu migration ile otomatik oluşturur.
Swagger: http://localhost:5131/swagger

## Endpoint'ler

| Metot | Adres | Açıklama |
|---|---|---|
| GET | /api/products | Filtreli ve sayfalı liste |
| GET | /api/products/{id} | Id'ye göre ürün |
| POST | /api/products | Yeni ürün ekler |
| PUT | /api/products/{id} | Ürünü günceller |
| DELETE | /api/products/{id} | Ürünü siler |

## Filtreleme ve sayfalama

`GET /api/products?name=vida&minPrice=1&maxPrice=5&minStock=100&page=2&pageSize=5`

| Parametre | Açıklama |
|---|---|
| name | Adında geçen yazı (büyük/küçük harf fark etmez) |
| minPrice / maxPrice | Fiyat aralığı |
| minStock / maxStock | Stok aralığı |
| page | Sayfa numarası (varsayılan 1) |
| pageSize | Sayfa boyutu, 1-100 (varsayılan 10) |

## Doğrulama kuralları
- Ürün adı boş olamaz, en fazla 100 karakter (400)
- Stok miktarı ve birim fiyat 0'dan küçük olamaz (400)
- Aynı adlı ürün eklenemez (409)
- Olmayan ürün için 404

## Testler

```
dotnet test
```

- **Unit:** controller testleri
- **Integration:** HTTP üzerinden uçtan uca akış
- **B2B:** partner firma istemcisinin API'yi tüketmesini simüle eden testler (sözleşme, katalog senkronizasyonu, hata durumları)

Testler InMemory veritabanı kullanır, PostgreSQL gerekmez.