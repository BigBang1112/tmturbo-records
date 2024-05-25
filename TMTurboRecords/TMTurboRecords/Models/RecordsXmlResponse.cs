using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Models;

public sealed class RecordsXmlResponse
{
    public DateTimeOffset? Timestamp { get; }
    public List<Record> Records { get; }
    public string? Error { get; }
    public double? RequestTime { get; }
    public double? ExecutionTime { get; }

    public RecordsXmlResponse(DateTimeOffset? timestamp, List<Record> records, string? error, double? requestTime, double? executionTime)
    {
        Timestamp = timestamp;
        Records = records;
        Error = error;
        RequestTime = requestTime;
        ExecutionTime = executionTime;
    }
}
