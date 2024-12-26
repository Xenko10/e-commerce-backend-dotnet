namespace e_commerce_backend_dotnet;

public class FlashSalesProduct
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
}