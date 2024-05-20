namespace TMTurboRecords.Shared.Models;

public sealed class RecordsResponse
{
    public required Dictionary<string, PlatformResponse>? Platforms { get; set; }
    public required RecordDistributionGraph? RecordDistributionGraph { get; set; }
    public required List<RankedRecord>? Records { get; set; }
}
