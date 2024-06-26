﻿namespace TMTurboRecords.Shared.Models;

public sealed class PlatformResponse
{
    public required int? Count { get; set; }
    public required DateTimeOffset? Timestamp { get; set; }
    public required double? RequestTime { get; set; }
    public required double? ExecutionTime { get; set; }
    public required string? Error { get; set; }
}
