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
            async Task<Results<Ok<string>, UnauthorizedHttpResult>> (SignInManager<IdentityUser> signInManager,
                LoginDto model) =>
            {
                if (!Validator.TryValidateObject(model, new ValidationContext(model), null, true))
                {
                    return TypedResults.Unauthorized();
                }

                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

                if (result.Succeeded)
                {
                    var token = GenerateJwtToken(model.Email);
                    return TypedResults.Ok(token);
                }

                return TypedResults.Unauthorized();
            });
    }

    private string GenerateJwtToken(string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET") ??
                                          throw new InvalidOperationException(
                                              "JWT_SECRET environment variable is not set"));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, email) }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = "http://localhost:3000/",
            Audience = "http://localhost:3000/"
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}