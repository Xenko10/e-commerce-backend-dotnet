using Ecommerce.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public static class FlashSalesProductsEndpoints
{
    public static void MapFlashSalesProductsEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/flash-sales-products", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
        {
                var flashSalesProducts = await db.FlashSalesProducts
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

                if (flashSalesProducts.Count == 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(flashSalesProducts);
            });

        routes.MapPost("/flash-sales-products/{productId:int}",
            async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId,
                CancellationToken ct) =>
            {
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var productInFlashSalesProduct =
                    await db.FlashSalesProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (productInFlashSalesProduct != null)
                {
                    return TypedResults.BadRequest();
                }

                var flashSalesProduct = new FlashSalesProduct
                {
                    ProductId = productId,
                    Product = product
                };

                db.FlashSalesProducts.Add(flashSalesProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/flash-sales-products/{flashSalesProduct.Id}", product);
            });

        routes.MapDelete("/flash-sales-products/{productId:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var flashSalesProduct =
                    await db.FlashSalesProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (flashSalesProduct is null)
                {
                    return TypedResults.NotFound();
                }

                db.FlashSalesProducts.Remove(flashSalesProduct);
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            });
    }
}