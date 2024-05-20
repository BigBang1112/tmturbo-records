using System.Text.Json.Serialization;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Shared;

[JsonSerializable(typeof(RecordsResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
    private static readonly System.Text.Json.JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };
    
    public static AppJsonSerializerContext Optimized { get; }

    static AppJsonSerializerContext()
    {
        jsonOptions.Converters.Add(new JsonRankedRecordConverter());
        Optimized = new AppJsonSerializerContext(jsonOptions);
    }
}
