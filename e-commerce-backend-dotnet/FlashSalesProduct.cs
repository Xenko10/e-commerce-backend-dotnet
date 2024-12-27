namespace Ecommerce;

public class FlashSalesProduct
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
}