using Ecommerce.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public static class ProductsEndpoints
{
    public static void MapProductsEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/products/{productId:int}", async Task<Results<Ok<Product>, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
        {
            var item = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken: ct);
            if (item is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(item);
        });

        routes.MapDelete("/products/{productId:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
        {
            var product = await db.Products.FindAsync(productId, ct);
            if (product is null)
            {
                return TypedResults.NotFound();
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync(ct);
            return TypedResults.NoContent();
        });

        // TODO add pagination (with validation)
        routes.MapGet("/products", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
        {
            var products = await db.Products.ToListAsync(ct);
            if (products.Count == 0)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(products);
        });

        routes.MapPost("/products", async Task<Results<Created<Product>, BadRequest>> (AppDbContext db, Product productDto, CancellationToken ct) =>
        {
            // Required data validation
            if (string.IsNullOrEmpty(productDto.Url) ||
                string.IsNullOrEmpty(productDto.Alt) ||
                string.IsNullOrEmpty(productDto.Header) ||
                productDto.Price <= 0 ||
                productDto.Stars <= 0 ||
                productDto.Opinions <= 0)
            {
                return TypedResults.BadRequest();
            }

            // Optional data validation
            if (productDto.PriceAfterDiscount < 0 || productDto.PriceAfterDiscount >= productDto.Price)
            {
                return TypedResults.BadRequest();
            }
             
            var product = new Product
            {
                Url = productDto.Url,
                Alt = productDto.Alt,
                Header = productDto.Header,
                Price = productDto.Price,
                PriceAfterDiscount = productDto.PriceAfterDiscount,
                Stars = productDto.Stars,
                Opinions = productDto.Opinions
            };
            
            db.Products.Add(product);
            var result = await db.SaveChangesAsync(ct);
            if (result == 0)
            {
                return TypedResults.BadRequest();
            }
            return TypedResults.Created($"/product/{product.Id}", product);
        });
    }
}