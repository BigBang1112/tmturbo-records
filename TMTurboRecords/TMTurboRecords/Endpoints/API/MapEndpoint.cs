using TMTurboRecords.Shared;

namespace TMTurboRecords.Endpoints.API;

public class MapEndpoint : IEndpoint
{
    public void RegisterEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/maps", () => Results.Ok(KnownMaps.ByUid)).WithOpenApi();
    }
}
