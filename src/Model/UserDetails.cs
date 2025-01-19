namespace Ecommerce.Model;

public class UserDetails
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public required string StreetAddress { get; set; }
    public required string City { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Email { get; set; }
}