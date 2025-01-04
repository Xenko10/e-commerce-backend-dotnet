using System.ComponentModel.DataAnnotations;

using Ecommerce.Dto;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.HttpResults;

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
            async Task<Results<Ok, UnauthorizedHttpResult>> (SignInManager<IdentityUser> signInManager,
                LoginDto model) =>
            {
                if (!Validator.TryValidateObject(model, new ValidationContext(model), null, true))
                {
                    return TypedResults.Unauthorized();
                }

                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);

                if (result.Succeeded)
                {
                    return TypedResults.Ok();
                }

                return TypedResults.Unauthorized();
            });
    }
}