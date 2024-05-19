using Microsoft.AspNetCore.Mvc;
using TMTurboRecords.Services;

namespace TMTurboRecords.Endpoints.API;

public class RecordEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/records", async (
            [FromServices] RecordService recordService,
            string mapUid,
            string platform,
            string? zone,
            CancellationToken cancellationToken) =>
        {
            var records = await recordService.GetRecordsAsync(mapUid, platform, zone, cancellationToken);
            return Results.Ok(records);
        }).WithOpenApi();
    }
}
