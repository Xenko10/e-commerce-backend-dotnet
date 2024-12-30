using System.Text.Json;
using System.Text.Json.Serialization;

using Ecommerce;
using Ecommerce.Api;
using Ecommerce.Model;

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
app.MapWishlistEndpoints();
app.MapCartEndpoints();

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
    if (products is null or { Count: 0 })
    {
        throw new InvalidOperationException("No products found in data.json");
    }

    await dbContext.Products.AddRangeAsync(products);
    await dbContext.SaveChangesAsync();
}