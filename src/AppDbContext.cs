using Microsoft.EntityFrameworkCore;

namespace Ecommerce;

public class AppDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<FlashSalesProduct> FlashSalesProducts { get; set; }
    public DbSet<WishlistProduct>Wishlist { get; set; }
    public DbSet<CartProduct> Cart {get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
}