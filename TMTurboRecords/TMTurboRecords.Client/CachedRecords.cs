using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Client;

public sealed record CachedRecords(DateTimeOffset RequestedAt, List<RankedRecord> Records);
