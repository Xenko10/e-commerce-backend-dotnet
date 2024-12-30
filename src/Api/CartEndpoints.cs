using Carter;

using Ecommerce.Dto;
using Ecommerce.Model;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class CartEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var cartModule = routes.MapGroup("/cart").WithTags("Cart");
        cartModule.MapGet("", async Task<Ok<List<ProductWithQuantityDto>>> (AppDbContext db, CancellationToken ct) =>
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

        cartModule.MapPost("/{productId:int}",
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

                var newCartProduct = new CartProduct { ProductId = productId, Product = product, Quantity = 1 };

                db.Cart.Add(newCartProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/cart/{newCartProduct.ProductId}", newCartProduct);
            });

        cartModule.MapPut("/{productId:int}/quantity/{quantity:int}",
            async Task<Results<NoContent, NotFound, BadRequest>> (AppDbContext db, int productId, int quantity,
                CancellationToken ct) =>
            {
                if (quantity < 1 || quantity > 10)
                {
                    return TypedResults.BadRequest();
                }

                var cartProduct = await db.Cart.Where(fsp => fsp.ProductId == productId)
                    .ExecuteUpdateAsync(x => x.SetProperty(p => p.Quantity, quantity), ct);
                if (cartProduct is 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.NoContent();
            });

        cartModule.MapDelete("/{productId:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var cartProduct = await db.Cart.Where(fsp => fsp.ProductId == productId)
                    .ExecuteDeleteAsync(cancellationToken: ct);

                if (cartProduct == 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.NoContent();
            });
    }
}