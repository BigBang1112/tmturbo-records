using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using TmEssentials;
using TMTurboRecords.Shared;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class RecordService
{
    private readonly IHttpClientFactory httpFactory;
    private readonly ZoneService zoneService;
    private readonly IMemoryCache cache;
    private readonly ILogger<RecordService> logger;

    public RecordService(IHttpClientFactory httpFactory, ZoneService zoneService, IMemoryCache cache, ILogger<RecordService> logger)
    {
        this.httpFactory = httpFactory;
        this.zoneService = zoneService;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<List<RankedRecord>> GetRecordsAsync(Platform platform, string mapUid, string? zone, CancellationToken cancellationToken)
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

        if (platformStrings.Length == 1)
        {
            var records = cache.Get<List<Record>>($"Records_{platform}_{mapUid}_{zone}");

            if (records is not null)
            {
                return records.Select(x => new RankedRecord(x.PlatformRank, x)).ToList();
            }
        }

        var rankedRecs = new List<RankedRecord>();

        var rank = 1;
        var prevTime = default(TimeInt32?);

        await foreach (var rec in GatherRecordsAsync(platformStrings, mapUid, zone, cancellationToken).OrderBy(x => x))
        {
            rankedRecs.Add(new RankedRecord(rank, rec));

            if (rec.Time != prevTime)
            {
                rank++;
            }

            prevTime = rec.Time;
        }

        return rankedRecs;
    }

    private async IAsyncEnumerable<Record> GatherRecordsAsync(string[] platformStrings, string mapUid, string zone, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recordTasks = new List<Task<List<Record>>>();
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
                var records = cache.Get<List<Record>>($"Records_{p}_{mapUid}_{zone}");

                if (records is not null)
                {
                    foreach (var record in records)
                    {
                        yield return record;
                    }
                    continue;
                }
            }

            var task = GetRecordsAsync(p, xmlRequest, cancellationToken);
            recordTasks.Add(task);
            platformDict.Add(task, p);
        }

        await Task.WhenAll(recordTasks);

        foreach (var task in recordTasks)
        {
            var list = await task;

            cache.Set($"Records_{platformDict[task]}_{mapUid}_{zone}", list, TimeSpan.FromMinutes(5));

            foreach (var record in list)
            {
                yield return record;
            }
        }
    }

    private async Task<List<Record>> GetRecordsAsync(string platformId, string xmlRequest, CancellationToken cancellationToken)
    {
        var platformEnum = Enum.Parse<Platform>(platformId);

        using var response = await httpFactory.CreateClient("001-" + platformId.ToLowerInvariant()).SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml")
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);

        var xml = XDocument.Parse(responseXml);

        var responseElement = xml.Element("r")?.Element("r");
        var contentElement = responseElement?.Element("c");

        if (contentElement is null)
        {
            if (responseElement?.Element("e") is XElement errorElement)
            {
                // Error
                logger.LogError("XML-RPC error: {Error}", errorElement.Element("m")?.Value);
            }

            return [];
        }

        var dateResponse = contentElement.Element("d")?.Value;

        var date = long.TryParse(dateResponse, out var dateLong)
            ? DateTimeOffset.FromUnixTimeSeconds(dateLong)
            : default(DateTimeOffset?);

        return contentElement.Elements("i")
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
    }
}
