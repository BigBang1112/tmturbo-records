namespace TMTurboRecords.Shared.Models;

public sealed class PlatformResponse
{
    public required int Count { get; set; }
    public required DateTimeOffset? Timestamp { get; set; }
    public required string? Error { get; set; }
}
