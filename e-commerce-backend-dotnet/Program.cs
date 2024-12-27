 using Ecommerce;
 using Microsoft.AspNetCore.Http.HttpResults;
 using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

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
}).RequireAuthorization();

// TODO add pagination + pagination validation + add canceltion token
app.MapGet("/products", (AppDbContext db) =>
{
    var items = db.Products.ToList();
    return items;
});

app.MapPost("/products", async (AppDbContext db, Product productDto) =>
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
    await db.SaveChangesAsync();
    return Results.Created($"/product/{product.Id}", product);
});

// add As split query 
app.MapGet("/flashsalesproducts", (AppDbContext db) =>
{
    var flashSalesProducts = db.FlashSalesProducts
        .Include(fsp => fsp.Product)
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
        .ToList();
    return flashSalesProducts;
});

app.MapPost("/flashsalesproduct", async (AppDbContext db, int productId) =>
{
    var product = await db.Products.FindAsync(productId);
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
    await db.SaveChangesAsync();
    return Results.Created($"/flashsalesproduct/{flashSalesProduct.Id}", flashSalesProduct);
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
}