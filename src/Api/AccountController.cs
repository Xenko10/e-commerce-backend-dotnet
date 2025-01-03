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
                if (!Validator.TryValidateObject(model, new ValidationContext(model), null, true))
                {
                    return TypedResults.BadRequest(new List<string> { "Invalid model state" });
                }

                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    return TypedResults.Ok();
                }

                var errors = result.Errors.Select(e => e.Description).ToList();
                return TypedResults.BadRequest(errors);
            });
    }
}