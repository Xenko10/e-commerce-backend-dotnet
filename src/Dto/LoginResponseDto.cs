namespace Ecommerce.Dto;

public class LoginResponseDto
{
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public required string Message { get; set; }
}