 using e_commerce_backend_dotnet;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();

app.MapGet("/", () => "Hello World!");

app.MapGet("/product", (AppDbContext db) =>
{
    var item = db.Products.FirstOrDefault(p => p.Id == 1);
    return item;
});

app.MapGet("/products", (AppDbContext db) =>
{
    var items = db.Products.ToList();
    return items;
});

app.MapPost("/product", async (AppDbContext db, Product productDto) =>
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

app.Run();