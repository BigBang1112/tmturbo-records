using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Xml.Linq;
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

    public async Task<List<Record>> GetRecordsAsync(Platform platform, string mapUid, string? zone, CancellationToken cancellationToken)
    {
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

        var records = cache.Get<List<Record>>($"Records_{platform}_{mapUid}_{zone}");

        if (records is not null)
        {
            return records;
        }

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

        using var response = await httpFactory.CreateClient("001-" + platform.ToString().ToLowerInvariant()).SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
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
                throw new Exception($"XML-RPC error: {errorElement.Element("m")?.Value}");
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
                    Rank = index + 1,
                    Time = timeMs == -1 ? null : new(timeMs),
                    Count = int.Parse(element.Element("c")?.Value ?? "0"),
                    Platform = platform
                };

                return record;
            })
            .ToList();
    }

}
