namespace e_commerce_backend_dotnet;

public class Product(int id, string url, string alt, string header, int price, int priceAfterDiscount, double stars, int opinions)
{
    public int Id { get; } = id;
    public string Url { get; } = url;
    public string Alt { get; } = alt;
    public string Header { get; } = header;
    public int Price { get; } = price;
    public int PriceAfterDiscount { get; } = priceAfterDiscount;
    public double Stars { get; } = stars;
    public int Opinions { get; } = opinions;
}
