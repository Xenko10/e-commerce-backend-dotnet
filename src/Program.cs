 using System.Text.Json;
 using System.Text.Json.Serialization;
 using Ecommerce;
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

app.MapGet("/products/{productId:int}", async Task<Results<Ok<Product>, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
{
    var item = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken: ct);
    if (item is null)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(item);
});

app.MapDelete("/products/{productId:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
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
app.MapGet("/products", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
{
    var products = await db.Products.ToListAsync(ct);
    if (products.Count == 0)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(products);
});

app.MapPost("/products", async Task<Results<Created<Product>, BadRequest>> (AppDbContext db, Product productDto, CancellationToken ct) =>
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

app.MapGet("/flash-sales-products", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
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

app.MapPost("/flash-sales-products/{productId:int}", async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId, CancellationToken ct) =>
{
    var product = await db.Products.FindAsync(productId, ct);
    if (product == null)
    {
        return TypedResults.NotFound();
    }

    var productInFlashSalesProduct = await db.FlashSalesProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
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

app.MapDelete("/flash-sales-products/{productId:int}", async Task<Results<NoContent, NotFound>> (AppDbContext db, int productId, CancellationToken ct) =>
{
    var flashSalesProduct = await db.FlashSalesProducts.FirstOrDefaultAsync(fsp => fsp.ProductId == productId, ct);
    if (flashSalesProduct is null)
    {
        return TypedResults.NotFound();
    }

    db.FlashSalesProducts.Remove(flashSalesProduct);
    await db.SaveChangesAsync(ct);
    return TypedResults.NoContent();
});

app.MapGet("/wishlist", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
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

    if (wishlist.Count == 0)
    {
        return TypedResults.NotFound();
    }

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

app.MapGet("/cart", async Task<Results<Ok<List<Product>>, NotFound>> (AppDbContext db, CancellationToken ct) =>
{
    var cart = await db.Cart
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

    if (cart.Count == 0)
    {
        return TypedResults.NotFound();
    }

    return TypedResults.Ok(cart);
});

app.MapPost("/cart/{productId:int}", async Task<Results<Created<Product>, NotFound, BadRequest>> (AppDbContext db, int productId, CancellationToken ct) =>
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
        Product = product
    };

    db.Cart.Add(newCartProduct);
    var result = await db.SaveChangesAsync(ct);
    if (result == 0)
    {
        return TypedResults.BadRequest();
    }
    return TypedResults.Created($"/cart/{newCartProduct.Id}", product);
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

