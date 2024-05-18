using TMTurboRecords.Endpoints.API;

namespace TMTurboRecords.Extensions;

internal static class EndpointServiceExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<IEndpoint, ZoneEndpoint>();
        services.AddSingleton<IEndpoint, MapEndpoint>();
        services.AddSingleton<IEndpoint, RecordEndpoint>();
        return services;
    }

    public static IEndpointRouteBuilder UseEndpoints(this IEndpointRouteBuilder app)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var endpoints = scope.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.RegisterEndpoints(app);
        }

        return app;
    }
}
