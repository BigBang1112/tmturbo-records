using LazyCache;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using TmEssentials;
using TMTurboRecords.Models;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class RecordService
{
    private readonly RequestService requestService;
    private readonly ZoneService zoneService;
    private readonly IAppCache cache;
    private readonly ILogger<RecordService> logger;

    public RecordService(RequestService requestService, ZoneService zoneService, IAppCache cache, ILogger<RecordService> logger)
    {
        this.requestService = requestService;
        this.zoneService = zoneService;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<RecordsResponse> GetRecordsAsync(Platform platform, string mapUid, string? zone, CancellationToken cancellationToken)
    {
        if ((uint)platform > 7)
        {
            throw new ArgumentException("Invalid platform", nameof(platform));
        }

        if (!RegexUtils.MapUidRegex().IsMatch(mapUid))
        {
            throw new ArgumentException("Invalid map UID", nameof(mapUid));
        }

        var zones = await zoneService.GetZonesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(zone))
        {
            zone = "World";
        }

        if (!zones.ContainsKey(zone))
        {
            throw new ArgumentException("Invalid zone", nameof(zone));
        }

        var platformStrings = platform.ToString().Split(", ");

        List<RankedRecord> rankedRecs;

        if (platformStrings.Length == 1)
        {
            var recordsResp = cache.Get<RecordsXmlResponse>($"Records_{platform}_{mapUid}_{zone}");

            if (recordsResp is not null)
            {
                rankedRecs = recordsResp.Records.Select(x => new RankedRecord(x.PlatformRank, x)).ToList();

                return new RecordsResponse
                {
                    Platforms = new()
                    {
                        [platform.ToString()] = new PlatformResponse
                        {
                            Count = recordsResp.Records.Count,
                            Timestamp = recordsResp.Timestamp,
                            Error = recordsResp.Error
                        }
                    },
                    RecordDistributionGraph = GetRecordDistributionGraph(rankedRecs),
                    Records = rankedRecs
                };
            }
        }

        rankedRecs = [];

        var rank = 1;
        var rankOffset = 0;
        var prevTime = default(TimeInt32?);

        await foreach (var rec in GatherRecordsAsync(platformStrings, mapUid, zone, cancellationToken).OrderBy(x => x))
        {
            if (rec.Time is null)
            {
                rankedRecs.Add(new RankedRecord(rank, rec));
                continue;
            }

            if (prevTime is not null && rec.Time == prevTime)
            {
                rankOffset++;
            }
            else
            {
                rank += rankOffset;
                rankOffset = 1;
            }

            prevTime = rec.Time;

            rankedRecs.Add(new RankedRecord(rank, rec));
        }

        var platformRespDict = new Dictionary<string, PlatformResponse>();
        var graph = new RecordDistributionGraph();

        foreach (var p in platformStrings)
        {
            var recordsResp = cache.Get<RecordsXmlResponse>($"Records_{p}_{mapUid}_{zone}");

            if (recordsResp is null)
            {
                continue;
            }

            platformRespDict[p] = new PlatformResponse
            {
                Count = recordsResp.Records.Sum(x => x.Count),
                Timestamp = recordsResp.Timestamp,
                Error = recordsResp.Error
            };
        }

        return new RecordsResponse
        {
            Records = rankedRecs,
            Platforms = platformRespDict,
            RecordDistributionGraph = GetRecordDistributionGraph(rankedRecs)
        };
    }

    private async IAsyncEnumerable<Record> GatherRecordsAsync(string[] platformStrings, string mapUid, string zone, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recordTasks = new List<Task<RecordsXmlResponse>>();
        var platformDict = new Dictionary<Task, string>();

        var xmlRequest = $@"<root>
    <game>
        <name>ManiaPlanet</name>
        <version>3.3.0</version>
        <build>2016-11-07_16_15</build>
        <title>TMTurbo@nadeolabs</title>
    </game>
    <request>
        <name>GetLeaderBoardSummary</name>
        <params>
            <t>Map</t>
            <c>TMTurbo@nadeolabs</c>
            <m>{mapUid}</m>
            <s>0</s>
            <z>{zone}</z>
        </params>
    </request>
</root>";

        foreach (var p in platformStrings)
        {
            if (platformStrings.Length > 1)
            {
                var recordsResp = cache.Get<RecordsXmlResponse>($"Records_{p}_{mapUid}_{zone}");

                if (recordsResp is not null)
                {
                    foreach (var record in recordsResp.Records)
                    {
                        yield return record;
                    }
                    continue;
                }
            }

            var task = Task.Run(() => GetRecordsAsync(Enum.Parse<Platform>(p), xmlRequest, cancellationToken), cancellationToken);
            await Task.Delay(100, cancellationToken);
            recordTasks.Add(task);
            platformDict.Add(task, p);
        }

        await Task.WhenAll(recordTasks);

        foreach (var task in recordTasks)
        {
            var response = await task;

            cache.Add($"Records_{platformDict[task]}_{mapUid}_{zone}", response, TimeSpan.FromMinutes(5));

            foreach (var record in response.Records)
            {
                yield return record;
            }
        }
    }

    private async Task<RecordsXmlResponse> GetRecordsAsync(Platform platformEnum, string xmlRequest, CancellationToken cancellationToken)
    {
        using var response = await requestService.RequestAsync(platformEnum, xmlRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return new RecordsXmlResponse(null, [], "Failed to get records");
        }

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

        var xml = XDocument.Parse(responseXml);

        var responseElement = xml.Element("r")?.Element("r");
        var contentElement = responseElement?.Element("c");

        if (contentElement is null)
        {
            var errorStr = "";

            if (responseElement?.Element("e") is XElement errorElement)
            {
                // Error
                errorStr = errorElement.Element("m")?.Value;
                logger.LogError("XML-RPC error: {Error}", errorStr);
            }

            return new RecordsXmlResponse(null, [], errorStr);
        }

        var dateResponse = contentElement.Element("d")?.Value;

        var date = long.TryParse(dateResponse, out var dateLong)
            ? DateTimeOffset.FromUnixTimeSeconds(dateLong)
            : default(DateTimeOffset?);

        var recs = contentElement.Elements("i")
            .Select((element, index) =>
            {
                var timeMs = (int)uint.Parse(element.Element("s")?.Value ?? "0");

                var record = new Record
                {
                    PlatformRank = index + 1,
                    Time = timeMs == -1 ? null : new(timeMs),
                    Count = int.Parse(element.Element("c")?.Value ?? "0"),
                    Platform = platformEnum
                };

                return record;
            })
            .ToList();

        return new RecordsXmlResponse(date, recs, null);
    }

    private static RecordDistributionGraph GetRecordDistributionGraph(List<RankedRecord> records)
    {
        var graph = new RecordDistributionGraph();

        if (records.Count == 0)
        {
            return graph;
        }

        var xs = new Dictionary<int, int>();

        var currentSecond = default(int?);

        foreach (var recs in records.GroupBy(x => x.Record.Platform))
        {
            if (!recs.Any())
            {
                continue;
            }

            var startingRec = recs.First();
            var offsetRec = startingRec;

            if (startingRec.Record.Time is null || offsetRec.Record.Time is null)
            {
                continue;
            }

            var recCount = 0;
            var recCountsPer100ms = new List<double>();
            var prevSec = startingRec.Record.Time.Value.TotalSeconds;

            var xIndex = 0;

            foreach (var record in recs)
            {
                if (record.Record.Time is null)
                {
                    break;
                }

                if (record.Record.Time.Value.TotalMilliseconds - startingRec.Record.Time.Value.TotalMilliseconds > 10000)
                {
                    break;
                }

                recCount += record.Record.Count;

                if (record.Record.Time.Value.TotalMilliseconds - offsetRec.Record.Time.Value.TotalMilliseconds > 100)
                {
                    recCountsPer100ms.Add(recCount);
                    xIndex++;
                    recCount = 0;
                    offsetRec = record;

                    var sec = (int)(record.Record.Time.Value.TotalSeconds);

                    if (currentSecond is null || sec > currentSecond)
                    {
                        if (sec > currentSecond)
                        {
                            xs[sec] = xIndex - 10; // idk why I do this
                            continue; //
                        }

                        currentSecond = sec;
                    }

                }
            }

            graph.Y[recs.Key.ToString()] = recCountsPer100ms.ToArray();
        }

        /*var chunking = 100;


        var xs = records.Select(x => x.Record.Time)
            .OfType<TimeInt32>()
            .Distinct()
            .Chunk(chunking)
            .Select(x => new TimeInt32((int)x.Average(x => x.TotalMilliseconds)))
            .Take(10);

        var x = new string[100];

        var counter = 0;

        foreach (var xVal in xs)
        {
            if (counter == 100)
            {
                x[counter - 1] = xVal.ToString();
                break;
            }

            x[counter] = xVal.ToString();
            counter += 10;
        }

        graph.X = x;

        graph.Y = records.GroupBy(x => x.Record.Platform).ToDictionary(
            x => x.Key.ToString(),
            x => x.Select(x => x.Record.Count).Chunk(chunking).Select(x => x.Average()).Take(100).ToArray()
        );*/

        graph.X = new string[graph.Y.Max(x => x.Value.Length)];

        var reverseDict = new Dictionary<int, int>();
        foreach (var pair in xs)
        {
            reverseDict[pair.Value] = pair.Key;
        }

        for (var i = 0; i < graph.X.Length; i++)
        {
            graph.X[i] = reverseDict.TryGetValue(i, out var x) ? TimeInt32.FromSeconds(x).ToString() : "";
        }

        return graph;
    }
}
