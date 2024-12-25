using e_commerce_backend_dotnet;

var builder = WebApplication.CreateBuilder(args);

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

app.UseCors();

app.MapGet("/", () => "Hello World!");

app.MapGet("/product", () =>
{
    var item = new Product(id: 1, url: "gamepad.png", alt: "gamepad", header: "HAVIT HV-G92 Gamepad", price: 160, priceAfterDiscount: 120, stars: 4.5, opinions: 88);
    return item;
});

app.MapGet("/products", () =>
{
    var items = new List<Product>
    {
        new Product(id: 1, url: "gamepad.png", alt: "gamepad", header: "HAVIT HV-G92 Gamepad", price: 160, priceAfterDiscount: 120, stars: 4.5, opinions: 88),
        new Product(id: 2, url: "keyboard.png", alt: "keyboard", header: "AK-900 Wired Keyboard", price: 1160, priceAfterDiscount: 920, stars: 4, opinions: 75),
        new Product(id: 3, url: "monitor.png", alt: "monitor", header: "IPS LCD Gaming Monitor", price: 400, priceAfterDiscount: 240, stars: 5, opinions: 121),
        new Product(id: 4, url: "chair.png", alt: "chair", header: "S-Series Comfort Chair", price: 400, priceAfterDiscount: 160, stars: 3.5, opinions: 99),
        new Product(id: 5, url: "laptop.png", alt: "laptop", header: "ASUS FHD Gaming Laptop", price: 700, priceAfterDiscount: 525, stars: 5, opinions: 325),
        new Product(id: 6, url: "camera.png", alt: "camera", header: "CANON EOS DSLR Camera", price: 360, priceAfterDiscount: 270, stars: 4, opinions: 95)
    };
    return items;
});


app.Run();