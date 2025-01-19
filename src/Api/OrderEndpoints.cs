using System.Security.Claims;

using Ecommerce.Dto;
using Ecommerce.Model;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Api;

public sealed class OrderEndpoints : IEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder routes)
    {
        var orderModule = routes.MapGroup("/orders").WithTags("Orders");

        orderModule.MapPost("/",
            async Task<Results<Created<object>, BadRequest>> (AppDbContext db, HttpContext httpContext,
                OrderDto orderDto,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return TypedResults.BadRequest();
                }

                var userDetails = new UserDetails
                {
                    UserId = userId,
                    Name = orderDto.Name,
                    StreetAddress = orderDto.StreetAddress,
                    City = orderDto.City,
                    PhoneNumber = orderDto.PhoneNumber,
                    Email = orderDto.Email
                };

                var order = new Order { UserId = userId, UserDetails = userDetails };

                var cartProducts = await db.Cart.Where(c => c.UserId == userId).ToListAsync(ct);
                if (!cartProducts.Any())
                {
                    return TypedResults.BadRequest();
                }

                foreach (var cartProduct in cartProducts)
                {
                    var productEntity = await db.Products.FindAsync(cartProduct.ProductId);
                    if (productEntity == null)
                    {
                        return TypedResults.BadRequest();
                    }

                    var orderProduct = new OrderProduct
                    {
                        Order = order,
                        ProductId = cartProduct.ProductId,
                        Product = productEntity,
                        Quantity = cartProduct.Quantity,
                    };
                    order.OrderProducts.Add(orderProduct);
                }

                db.Orders.Add(order);
                db.Cart.RemoveRange(cartProducts);
                var result = await db.SaveChangesAsync(ct);
                if (result == 0)
                {
                    return TypedResults.BadRequest();
                }

                return TypedResults.Created<object>($"/orders/{order.Id}", new { OrderId = order.Id });
            });

        orderModule.MapGet("/",
            async Task<Results<Ok<List<Order>>, NotFound>> (AppDbContext db, HttpContext httpContext,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return TypedResults.NotFound();
                }

                var orders = await db.Orders
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderProducts)
                    .ToListAsync(ct);

                if (!orders.Any())
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(orders);
            });

        orderModule.MapGet("/{orderId}",
            async Task<Results<Ok<Order>, NotFound>> (AppDbContext db, HttpContext httpContext, int orderId,
                CancellationToken ct) =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                {
                    return TypedResults.NotFound();
                }

                var order = await db.Orders
                    .Where(o => o.UserId == userId && o.Id == orderId)
                    .Include(o => o.OrderProducts)
                    .FirstOrDefaultAsync(ct);

                if (order == null)
                {
                    return TypedResults.NotFound();
                }

                return TypedResults.Ok(order);
            });
    }
}