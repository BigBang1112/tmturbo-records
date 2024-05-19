using TmEssentials;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class RecordService
{
    public async Task<List<Record>> GetRecordsAsync(string mapUid, string? platform, string? zone, CancellationToken cancellationToken)
    {
        await Task.Delay(3000, cancellationToken);

        var platformEnum = platform switch
        {
            "PC" => Platform.PC,
            "XB1" => Platform.XB1,
            "PS4" => Platform.PS4,
            _ => Platform.None
        };

        return [new Record() { Rank = 1, Time = TimeInt32.MaxValue, Platform = platformEnum }];
    }
}
