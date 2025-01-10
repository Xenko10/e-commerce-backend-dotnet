using Ecommerce.Model;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class WishlistEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var wishlistModule = routes.MapGroup("/wishlist").WithTags("Wishlist");

        wishlistModule.MapGet("", async Task<Ok<List<Product>>> (AppDbContext db, CancellationToken ct) =>
        {
            var wishlist = await db.Wishlist.AsNoTracking()
                .Include(fsp => fsp.Product)
                .AsSplitQuery()
                .Select(fsp => new Product
                {
                    Id = fsp.Product.Id,
                    Url = fsp.Product.Url,
                    Alt = fsp.Product.Alt,
                    Header = fsp.Product.Header,
                    Price = fsp.Product.Price,
                    PriceAfterDiscount = fsp.Product.PriceAfterDiscount,
                    Stars = fsp.Product.Stars,
                    Opinions = fsp.Product.Opinions
                })
                .ToListAsync(ct);
            return TypedResults.Ok(wishlist);
        });

        wishlistModule.MapPost("/{productId:int}",
            async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId,
                CancellationToken ct) =>
            {
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var productInWishlist = await db.Wishlist.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (productInWishlist != null)
                {
                    return TypedResults.BadRequest();
                }

                var newWishlistProduct = new WishlistProduct { ProductId = productId, Product = product };

                db.Wishlist.Add(newWishlistProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/{newWishlistProduct.Id}", product);
            });

        wishlistModule.MapDelete("/{productId:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var wishlistProduct = await db.Wishlist.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (wishlistProduct is null)
                {
                    return TypedResults.NotFound();
                }

                db.Wishlist.Remove(wishlistProduct);
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            });
    }
}