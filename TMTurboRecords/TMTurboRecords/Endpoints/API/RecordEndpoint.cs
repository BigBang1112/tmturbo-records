using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TMTurboRecords.Services;
using TMTurboRecords.Shared;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Endpoints.API;

public class RecordEndpoint : IEndpoint
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    static RecordEndpoint()
    {
        jsonSerializerOptions.Converters.Add(new JsonRankedRecordConverter());
    }

    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/records/{platform}/{mapUid}/{zone}", async (
            [FromServices] RecordService recordService,
            Platform platform,
            string mapUid,
            string? zone = null,
            bool verbose = false,
            CancellationToken cancellationToken = default) =>
        {
            var records = await recordService.GetRecordsAsync(platform, mapUid, zone, cancellationToken);
            
            if (verbose)
            {
                return Results.Ok(records);
            }

            return Results.Json(records, jsonSerializerOptions);
        }).WithOpenApi();
    }
}
