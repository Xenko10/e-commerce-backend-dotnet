using Ecommerce.Dto;
using Ecommerce.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/cart", async Task<Ok<List<ProductWithQuantityDto>>> (AppDbContext db, CancellationToken ct) =>
        {
            var cart = await db.Cart
                .Include(fsp => fsp.Product)
                .AsSplitQuery()
                .Select(fsp => new ProductWithQuantityDto
                {
                    Id = fsp.Product.Id,
                    Url = fsp.Product.Url,
                    Alt = fsp.Product.Alt,
                    Header = fsp.Product.Header,
                    Price = fsp.Product.Price,
                    PriceAfterDiscount = fsp.Product.PriceAfterDiscount,
                    Stars = fsp.Product.Stars,
                    Opinions = fsp.Product.Opinions,
                    Quantity = fsp.Quantity
                })
                .ToListAsync(ct);

            return TypedResults.Ok(cart);
        });

        routes.MapPost("/cart/{productId:int}",
            async Task<Results<Created<CartProduct>, NotFound, BadRequest>> (AppDbContext db, int productId,
                CancellationToken ct) =>
            {
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var productInCart = await db.Cart.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (productInCart != null)
                {
                    return TypedResults.BadRequest();
                }

                var newCartProduct = new CartProduct
                {
                    ProductId = productId,
                    Product = product,
                    Quantity = 1
                };

                db.Cart.Add(newCartProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/cart/{newCartProduct.ProductId}", newCartProduct);
            });

        routes.MapPut("/cart/{productId:int}/quantity/{quantity:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, int quantity,
                CancellationToken ct) =>
            {
                var cartProduct = await db.Cart.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (cartProduct is null)
                {
                    return TypedResults.NotFound();
                }

                cartProduct.Quantity = quantity;
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            });

        routes.MapDelete("/cart/{productId:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var cartProduct = await db.Cart.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (cartProduct is null)
                {
                    return TypedResults.NotFound();
                }

                db.Cart.Remove(cartProduct);
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            });
    }
}