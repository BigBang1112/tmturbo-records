using TMTurboRecords.Services;

namespace TMTurboRecords.Endpoints.API;

public class ZoneEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/zones", async (ZoneService zoneService, CancellationToken cancellationToken) =>
        {
            var zones = await zoneService.GetZonesAsync(cancellationToken);
            return Results.Ok(zones);
        }).WithOpenApi();
    }
}
