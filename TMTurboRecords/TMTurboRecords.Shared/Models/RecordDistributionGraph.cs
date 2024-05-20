namespace TMTurboRecords.Shared.Models;

public sealed class RecordDistributionGraph
{
    public string?[]? X { get; set; }
    public Dictionary<string, double[]> Y { get; set; } = [];
}
