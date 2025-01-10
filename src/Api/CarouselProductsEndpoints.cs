using Ecommerce.Model;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class CarouselProductsEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var carouselProductsModule = routes.MapGroup("/carousel").WithTags("Carousel products");
        carouselProductsModule.MapGet("",
            async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
            {
                var flashSalesProducts = await db.CarouselProducts.AsNoTracking()
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


        carouselProductsModule.MapGet("/flash-sales",
            async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
            {
                var flashSalesProducts = await db.CarouselProducts.AsNoTracking()
                    .Include(fsp => fsp.Product)
                    .AsSplitQuery()
                    .Where(fsp => fsp.IsFlashSalesProduct)
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

        carouselProductsModule.MapGet("/bestsellers",
            async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
            {
                var bestsellerProducts = await db.CarouselProducts.AsNoTracking()
                    .Include(fsp => fsp.Product)
                    .AsSplitQuery()
                    .Where(fsp => fsp.IsBestsellersProduct)
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

                if (bestsellerProducts.Count == 0)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(bestsellerProducts);
            });


        carouselProductsModule.MapPost("/{productId:int}",
            async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId,
                CarouselProductDto dto, CancellationToken ct) =>
            {
                var product = await db.Products.FindAsync(productId, ct);
                if (product == null)
                {
                    return TypedResults.NotFound();
                }

                var isCarouselProduct =
                    await db.CarouselProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (isCarouselProduct != null)
                {
                    return TypedResults.BadRequest();
                }

                var carouselProduct =
                    new CarouselProduct(productId, product, dto.IsFlashSalesProduct, dto.IsBestsellerProduct)
                    {
                        Product = product
                    };

                db.CarouselProducts.Add(carouselProduct);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created($"/{carouselProduct.ProductId}", product);
            });

        carouselProductsModule.MapDelete("/{productId:int}",
            async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
            {
                var carouselProduct =
                    await db.CarouselProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
                if (carouselProduct is null)
                {
                    return TypedResults.NotFound();
                }

                db.CarouselProducts.Remove(carouselProduct);
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            });
    }
}