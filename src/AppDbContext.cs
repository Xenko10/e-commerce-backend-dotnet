using Ecommerce.Model;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
    public DbSet<CarouselProduct> CarouselProducts { get; set; }
    public DbSet<WishlistProduct> Wishlist { get; set; }
    public DbSet<CartProduct> Cart { get; set; }
}