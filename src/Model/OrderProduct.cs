using System.Text.Json.Serialization;

namespace Ecommerce.Model;

public class OrderProduct
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    [JsonIgnore] public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public required Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}