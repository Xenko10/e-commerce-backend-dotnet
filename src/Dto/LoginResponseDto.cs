namespace Ecommerce.Dto;

public class LoginResponseDto
{
    public required string UserId { get; set; }
    public string Token { get; set; }
    public string Message { get; set; }
}