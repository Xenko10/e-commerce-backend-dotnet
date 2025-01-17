using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Text;

using DotNetEnv;

using Ecommerce;
using Ecommerce.Extensions;
using Ecommerce.Model;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddOpenApi();

builder.Services.AddDbContextPool<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServerConnection")));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
});

Env.Load();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var key = Encoding.ASCII.GetBytes(jwtSecret ?? throw new InvalidOperationException());

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = "http://localhost:3000",
        ValidAudience = "http://localhost:3000",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.AddEndpoints();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UseCors(cors => cors.AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod());

app.MapEndpoints();

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