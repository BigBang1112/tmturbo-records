using TmEssentials;

namespace TMTurboRecords.Shared.Models;

public readonly record struct Record
{
    public int Rank { get; init; }
    public TimeInt32? Time { get; init; }
    public int Count { get; init; }
    public Platform Platform { get; init; }
}
