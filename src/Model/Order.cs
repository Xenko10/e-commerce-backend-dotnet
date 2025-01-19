namespace Ecommerce.Model;

public class Order
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required UserDetails UserDetails { get; set; }
    public List<OrderProduct> OrderProducts { get; set; } = new();
}