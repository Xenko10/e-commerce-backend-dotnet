using Ecommerce.Api;

namespace Ecommerce.Extensions;

public static class EndpointExtensions
{
    public static WebApplicationBuilder AddEndpoints(this WebApplicationBuilder builder)
    {
        foreach (var endpoint in typeof(Program).Assembly.GetTypes().Where(t =>
                     !t.IsAbstract &&
                     typeof(IEndpoint).IsAssignableFrom(t) &&
                     t != typeof(IEndpoint) &&
                     (t.IsPublic || t.IsNestedPublic)
                 ))
        {
            builder.Services.AddSingleton(typeof(IEndpoint), endpoint);
        }

        return builder;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        foreach (var endpoint in app.Services.GetServices<IEndpoint>())
        {
            endpoint.AddRoutes(app);
        }

        return app;
    }
}