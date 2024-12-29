namespace Ecommerce;

public class CartProduct
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public required Product Product { get; init; }
    public int Quantity { get; set; }

}

public class ProductWithQuantity : Product
{
    public new int Id { get; init; }
    public int Quantity { get; set; }
}