using System.Security.Claims;

using Ecommerce.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class WishlistEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var wishlistModule = routes.MapGroup("/wishlist").WithTags("Wishlist");

        wishlistModule.MapGet("", [Authorize]
            async Task<Ok<List<Product>>> (HttpContext httpContext, AppDbContext db, CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var wishlist = await db.Wishlist.AsNoTracking()
                    .Where(wp => wp.UserId == userId)
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

        wishlistModule.MapPost("/{productId:int}", [Authorize]
            async Task<Results<Created<Product>, NotFound, BadRequest>> (HttpContext httpContext, AppDbContext db,
                int productId, CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var productInWishlist =
                    await db.Wishlist.FirstOrDefaultAsync(fsp => fsp.ProductId == productId && fsp.UserId == userId,
                        ct);
                if (productInWishlist != null)
                {
                    return TypedResults.BadRequest();
                }

                if (userId is null)
                {
                    return TypedResults.BadRequest();
                }

                var newWishlistProduct =
                    new WishlistProduct { ProductId = productId, Product = product, UserId = userId };

                db.Wishlist.Add(newWishlistProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/{newWishlistProduct.Id}", product);
            });

        wishlistModule.MapDelete("/{productId:int}", [Authorize]
            async Task<Results<NoContent, NotFound>> (HttpContext httpContext, AppDbContext db, int productId,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var wishlistProduct =
                    await db.Wishlist.FirstOrDefaultAsync(fsp => fsp.ProductId == productId && fsp.UserId == userId,
                        ct);
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