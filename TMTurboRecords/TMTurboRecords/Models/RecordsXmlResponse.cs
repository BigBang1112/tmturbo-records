using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Models;

public sealed class RecordsXmlResponse
{
    public DateTimeOffset? Timestamp { get; }
    public List<Record> Records { get; }
    public string? Error { get; }

    public RecordsXmlResponse(DateTimeOffset? timestamp, List<Record> records, string? error)
    {
        Timestamp = timestamp;
        Records = records;
        Error = error;
    }
}
