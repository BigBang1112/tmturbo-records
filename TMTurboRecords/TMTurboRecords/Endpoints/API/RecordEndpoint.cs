using Microsoft.AspNetCore.Mvc;
using TMTurboRecords.Services;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Endpoints.API;

public class RecordEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/records/{platform}/{mapUid}/{zone?}", async (
            [FromServices] RecordService recordService,
            Platform platform,
            string mapUid,
            string? zone,
            CancellationToken cancellationToken) =>
        {
            var records = await recordService.GetRecordsAsync(platform, mapUid, zone, cancellationToken);
            return Results.Ok(records);
        }).WithOpenApi();
    }
}
