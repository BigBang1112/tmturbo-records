using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Text.Json;
using TmEssentials;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Shared;

public sealed class JsonRankedRecordConverter : JsonConverter<RankedRecord>
{
    public override RankedRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Debug.Assert(typeToConvert == typeof(RankedRecord));

        reader.Read();
        var rank = reader.GetInt32();
        reader.Read();
        var platformRank = reader.GetInt32();
        reader.Read();
        var time = reader.GetInt32();
        reader.Read();
        var count = reader.GetInt32();
        reader.Read();
        var platform = reader.GetInt32();
        reader.Read();

        return new RankedRecord(rank, new Record(platformRank, time == -1 ? null : new TimeInt32(time), count, (Platform)platform));
    }

    public override void Write(Utf8JsonWriter writer, RankedRecord value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Rank);
        writer.WriteNumberValue(value.Record.PlatformRank);
        writer.WriteNumberValue(value.Record.Time?.TotalMilliseconds ?? -1);
        writer.WriteNumberValue(value.Record.Count);
        writer.WriteNumberValue((int)value.Record.Platform);
        writer.WriteEndArray();
    }
}
