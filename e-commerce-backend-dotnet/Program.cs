using e_commerce_backend_dotnet;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.MapGet("/item", () =>
{
    var item = new Item(Id: 1, Name: "isana");
    return item;
});

app.MapGet("/items", () =>
{
    var items = new List<Item>
    {
        new Item(1, "isana"),
        new Item(2, "dove"),
        new Item(3, "nivea")
    };
    return items;
});

app.Run();