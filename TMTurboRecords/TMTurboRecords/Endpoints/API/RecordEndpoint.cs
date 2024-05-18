using TMTurboRecords.Services;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Endpoints.API;

public class RecordEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/records", async (ZoneService zoneService, CancellationToken cancellationToken) =>
        {
            await Task.Delay(3000, cancellationToken);
            return Results.Ok(new List<Record>());
        }).WithOpenApi();
    }
}
