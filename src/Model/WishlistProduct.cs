namespace Ecommerce.Model;

public class WishlistProduct
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public required Product Product { get; init; }
    public required string UserId { get; set; }
}