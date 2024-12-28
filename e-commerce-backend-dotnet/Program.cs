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

app.MapPost("/products", async (AppDbContext db, Product productDto, CancellationToken ct) =>
{
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
    await db.SaveChangesAsync(ct);
    return Results.Created($"/product/{product.Id}", product);
});

app.MapGet("/flash-sales-products", async (AppDbContext db, CancellationToken ct) =>
{
    var flashSalesProducts = await db.FlashSalesProducts
        .Include(fsp => fsp.Product)
        .AsSplitQuery()
        .Select(fsp => new
        {
            fsp.Product.Id,
            fsp.Product.Url,
            fsp.Product.Alt,
            fsp.Product.Header,
            fsp.Product.Price,
            fsp.Product.PriceAfterDiscount,
            fsp.Product.Stars,
            fsp.Product.Opinions
        })
        .ToListAsync(ct);
    return flashSalesProducts;
});

app.MapPost("/flash-sales-products/{productId:int}", async (AppDbContext db, int productId, CancellationToken ct) =>
{
    var product = await db.Products.FindAsync(productId, ct);
    if (product == null)
    {
        return Results.NotFound("Product not found");
    }

    var flashSalesProduct = new FlashSalesProduct
    {
        ProductId = productId,
        Product = product
    };

    db.FlashSalesProducts.Add(flashSalesProduct);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/flash-sales-products/{flashSalesProduct.Id}", flashSalesProduct);
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

