using Ecommerce.Dto;
using Ecommerce.Model;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class ProductsEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var productsModule = routes.MapGroup("/products").WithTags("Products");

        productsModule.MapGet("/{productId:int}",
            async Task<Results<Ok<Product>, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var item = await db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken: ct);
                if (item is null)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(item);
            });

        productsModule.MapGet("",
            async Task<Results<Ok<PagedResult<Product>>, BadRequest, NotFound>> (AppDbContext db, CancellationToken ct,
                int page = 1, int pageSize = 8) =>
            {
                if (page <= 0 || pageSize <= 0)
                {
                    return TypedResults.BadRequest();
                }

                var totalProducts = await db.Products
                    .AsNoTracking()
                    .CountAsync(ct);
                var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

                if (page > totalPages)
                {
                    return TypedResults.NotFound();
                }

                var products = await db.Products
                    .AsNoTracking()
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                if (products.Count == 0)
                {
                    return TypedResults.NotFound();
                }

                var result = new PagedResult<Product> { Items = products, TotalCount = totalProducts };

                return TypedResults.Ok(result);
            });

        productsModule.MapPost("",
            async Task<Results<Created<Product>, BadRequest>> (AppDbContext db, Product productDto,
                CancellationToken ct) =>
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