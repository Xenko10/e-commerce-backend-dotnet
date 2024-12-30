 using System.Text.Json;
 using System.Text.Json.Serialization;
 using Ecommerce;
 using Ecommerce.Api;
 using Ecommerce.Dto;
 using Ecommerce.Model;
 using Microsoft.AspNetCore.Http.HttpResults;
 using Microsoft.EntityFrameworkCore;
 using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddCors();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(cors => cors.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.MapProductsEndpoints();
app.MapFlashSalesProductsEndpoints();

app.MapGet("/wishlist", async Task<Ok<List<Product>>> (AppDbContext db, CancellationToken ct) =>
{
    var wishlist = await db.Wishlist
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

app.MapPost("/wishlist/{productId:int}", async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId, CancellationToken ct) =>
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
    
    var newWishlistProduct = new WishlistProduct
    {
        ProductId = productId,
        Product = product
    };

    db.Wishlist.Add(newWishlistProduct);
    var result = await db.SaveChangesAsync(ct);
    if (result == 0)
    {
        return TypedResults.BadRequest();
    }
    return TypedResults.Created($"/wishlist/{newWishlistProduct.Id}", product);
});

app.MapDelete("/wishlist/{productId:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
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

app.MapGet("/cart", async Task<Ok<List<ProductWithQuantityDto>>> (AppDbContext db, CancellationToken ct) =>
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

app.MapPost("/cart/{productId:int}", async Task<Results<Created<CartProduct>, NotFound, BadRequest>> (AppDbContext db, int productId, CancellationToken ct) =>
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

app.MapPut("/cart/{productId:int}/quantity/{quantity:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, int quantity, CancellationToken ct) =>
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

app.MapDelete("/cart/{productId:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
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

await Migrate(app);

app.Run();

async Task Migrate(WebApplication webApplication)
{
    await using var scope = webApplication.Services.CreateAsyncScope();
    await using var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
    
    var productsExist = await dbContext.Products.AnyAsync();
    if (productsExist)
    {
        return;
    }
    await using var file = File.OpenRead("data.json");
    var products = await JsonSerializer.DeserializeAsync<List<Product>>(file, JsonSerializerOptions.Web);
    if (products is null or {Count: 0})
    {
        throw new InvalidOperationException("No products found in data.json");
    }
    
    await dbContext.Products.AddRangeAsync(products);
    await dbContext.SaveChangesAsync();
}

