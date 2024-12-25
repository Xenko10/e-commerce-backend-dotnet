using e_commerce_backend_dotnet;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.MapGet("/item", () =>
{
    var item = new Item(id: 1, url: "gamepad.png", alt: "gamepad", header: "HAVIT HV-G92 Gamepad", price: 160, priceAfterDiscount: 120, stars: 4.5, opinions: 88);
    return item;
});

app.MapGet("/items", () =>
{
    var items = new List<Item>
    {
        new Item(id: 1, url: "gamepad.png", alt: "gamepad", header: "HAVIT HV-G92 Gamepad", price: 160, priceAfterDiscount: 120, stars: 4.5, opinions: 88),
        new Item(id: 2, url: "keyboard.png", alt: "keyboard", header: "AK-900 Wired Keyboard", price: 1160, priceAfterDiscount: 920, stars: 4, opinions:75)
    };
    return items;
});

app.Run();