namespace Ecommerce;

public class FlashSalesProduct
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public required Product Product { get; init; }
}