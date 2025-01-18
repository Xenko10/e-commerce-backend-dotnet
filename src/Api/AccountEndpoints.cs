using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Ecommerce.Dto;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.IdentityModel.Tokens;

namespace Ecommerce.Api;

public sealed class AccountEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var accountModule = routes.MapGroup("/account").WithTags("Account");

        accountModule.MapPost("/register",
            async Task<Results<Ok, BadRequest<List<string>>>> (UserManager<IdentityUser> userManager,
                RegisterDto model) =>
            {
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(model, new ValidationContext(model), validationResults, true))
                {
                    var validationErrors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown error").ToList();
                    return TypedResults.BadRequest(validationErrors);
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return TypedResults.Ok();
                }

                var creationErrors = result.Errors.Select(e => e.Description).ToList();
                return TypedResults.BadRequest(creationErrors);
            });


        accountModule.MapPost("/login",
            async Task<Results<Ok<LoginResponseDto>, UnauthorizedHttpResult>> (
                SignInManager<IdentityUser> signInManager,
                UserManager<IdentityUser> userManager, LoginDto model) =>
            {
                if (!Validator.TryValidateObject(model, new ValidationContext(model), null, true))
                {
                    return TypedResults.Unauthorized();
                }

                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

                if (result.Succeeded)
                {
                    var user = await userManager.FindByEmailAsync(model.Email);
                    var token = GenerateJwtToken(user ?? throw new InvalidOperationException());

                    var response =
                        new LoginResponseDto {  Token = token, Message = "Login successful" };
                    return TypedResults.Ok(response);
                }

                return TypedResults.Unauthorized();
            });
    }

    private string GenerateJwtToken(IdentityUser user)
    {
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret ?? throw new InvalidOperationException()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id), new Claim(JwtRegisteredClaimNames.Email, user.Email ?? throw new InvalidOperationException()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "http://localhost:3000",
            audience: "http://localhost:3000",
            claims: claims,
            expires: DateTime.Now.AddDays(31),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}