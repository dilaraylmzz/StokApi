using Microsoft.EntityFrameworkCore;
using StokApi.Models;

namespace StokApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Name).IsRequired().HasMaxLength(100);
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(p => p.Name).IsUnique();
        });
    }
}