using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Text;
using System.Xml.Linq;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class ZoneService
{
    private readonly IHttpClientFactory httpFactory;
    private readonly IMemoryCache cache;
    private readonly ILogger<ZoneService> logger;

    public ZoneService(IHttpClientFactory httpFactory, IMemoryCache cache, ILogger<ZoneService> logger)
    {
        this.httpFactory = httpFactory;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<Dictionary<string, Zone>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        var zones = cache.Get<Dictionary<string, Zone>>("Zones");

        if (zones is not null)
        {
            return zones;
        }

        const string xmlRequest = @"<root>
    <game>
        <name>ManiaPlanet</name>
        <version>3.3.0</version>
        <build>2016-11-07_16_15</build>
    </game>
    <request>
        <name>GetLeagues</name>
    </request>
</root>";


        var requests = new Dictionary<string, Task<HttpResponseMessage>>(MasterServer.Platforms.Count);

        var platformRequests = MasterServer.Platforms
            .ToDictionary(
                platform => platform,
                // Only check from the first master server of the platform
                platform => httpFactory.CreateClient("001-" + platform).SendAsync(new HttpRequestMessage(HttpMethod.Post, "")
                {
                    Content = new StringContent(xmlRequest, Encoding.UTF8, "application/xml")
                }, cancellationToken)
            );

        await Task.WhenAll(platformRequests.Values);

        zones = [];

        foreach (var (platformName, responseTask) in platformRequests)
        {
            if (!responseTask.Result.IsSuccessStatusCode)
            {
                continue;
            }

            var responseXml = await responseTask.Result.Content.ReadAsStringAsync(cancellationToken);

            var xml = XDocument.Parse(responseXml);

            var responseElement = xml.Element("r")?.Element("r");
            var contentElement = responseElement?.Element("c");

            if (contentElement is null)
            {
                if (responseElement?.Element("e") is XElement errorElement)
                {
                    // Error
                    logger.LogError("XML-RPC error: {Error}", errorElement.Element("m")?.Value);
                    continue;
                }

                continue;
            }

            var platform = platformName switch
            {
                "pc" => Platform.PC,
                "xb1" => Platform.XB1,
                "ps4" => Platform.PS4,
                _ => Platform.None
            };

            foreach (var zoneElement in contentElement.Elements("l"))
            {
                var name = zoneElement.Element("a")?.Value ?? string.Empty;
                var parent = zoneElement.Element("b")?.Value;

                var fullZoneId = string.IsNullOrWhiteSpace(parent) ? name : $"{parent}|{name}";

                if (zones.TryGetValue(fullZoneId, out Zone? value))
                {
                    value.Platforms |= platform;
                    continue;
                }

                var zone = new Zone
                {
                    DisplayName = fullZoneId,
                    FlagUrl = null,
                    Platforms = platform
                };

                zones[fullZoneId] = zone;
            }

            responseTask.Result.Dispose();
        }

        cache.Set("Zones", zones, TimeSpan.FromDays(1));

        return zones;
    }
}
