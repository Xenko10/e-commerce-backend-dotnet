using System.Security.Claims;

using Ecommerce.Dto;
using Ecommerce.Model;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class CartEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var cartModule = routes.MapGroup("/cart").WithTags("Cart");

        cartModule.MapGet("", [Authorize]
            async Task<Ok<List<ProductWithQuantityDto>>> (HttpContext httpContext, AppDbContext db,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cart = await db.Cart.AsNoTracking()
                    .Where(cp => cp.UserId == userId)
                    .Include(cp => cp.Product)
                    .AsSplitQuery()
                    .Select(cp => new ProductWithQuantityDto
                    {
                        Id = cp.Product.Id,
                        Url = cp.Product.Url,
                        Alt = cp.Product.Alt,
                        Header = cp.Product.Header,
                        Price = cp.Product.Price,
                        PriceAfterDiscount = cp.Product.PriceAfterDiscount,
                        Stars = cp.Product.Stars,
                        Opinions = cp.Product.Opinions,
                        Quantity = cp.Quantity
                    })
                    .ToListAsync(ct);

                return TypedResults.Ok(cart);
            });

        cartModule.MapPost("/{productId:int}", [Authorize]
            async Task<Results<Created<CartProduct>, NotFound, BadRequest>> (HttpContext httpContext, AppDbContext db,
                int productId, CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var productInCart =
                    await db.Cart.FirstOrDefaultAsync(cp => cp.ProductId == productId && cp.UserId == userId, ct);
                if (productInCart != null)
                {
                    return TypedResults.BadRequest();
                }

                if (userId is null)
                {
                    return TypedResults.BadRequest();
                }

                var newCartProduct = new CartProduct
                {
                    ProductId = productId, Product = product, Quantity = 1, UserId = userId
                };

                db.Cart.Add(newCartProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/cart/{newCartProduct.ProductId}", newCartProduct);
            });

        cartModule.MapPut("/{productId:int}/quantity/{quantity:int}", [Authorize]
            async Task<Results<NoContent, NotFound, BadRequest>> (HttpContext httpContext, AppDbContext db,
                int productId, int quantity, CancellationToken ct) =>
            {
                if (quantity < 1 || quantity > 10)
                {
                    return TypedResults.BadRequest();
                }

                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartProduct = await db.Cart.Where(cp => cp.ProductId == productId && cp.UserId == userId)
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.Quantity, quantity), ct);
                if (cartProduct is 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.NoContent();
            });

        cartModule.MapDelete("/{productId:int}", [Authorize]
            async Task<Results<NoContent, NotFound>> (HttpContext httpContext, AppDbContext db, int productId,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var cartProduct = await db.Cart.Where(cp => cp.ProductId == productId && cp.UserId == userId)
                    .ExecuteDeleteAsync(cancellationToken: ct);

                if (cartProduct == 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.NoContent();
            });
    }
}