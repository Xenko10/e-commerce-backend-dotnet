namespace Ecommerce;

public class Product
{
    public int Id { get; init;}
    public required string Url { get; init;}
    public required string Alt { get; init;}
    public required string Header { get; init;}
    public int Price { get; init;}
    public int? PriceAfterDiscount { get; init;}
    public double Stars { get; init;}
    public int Opinions { get; init;}
}
