using Microsoft.Extensions.Caching.Memory;
using System.Xml.Linq;
using TMTurboRecords.Extensions;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class ZoneService
{
    private readonly RequestService requestService;
    private readonly IMemoryCache cache;
    private readonly ILogger<ZoneService> logger;

    public ZoneService(RequestService requestService, IMemoryCache cache, ILogger<ZoneService> logger)
    {
        this.requestService = requestService;
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

        var requests = new Dictionary<string, Task<HttpResponseMessage>>(PlatformExtensions.Platforms.Count());

        var platformRequests = PlatformExtensions.Platforms
            .ToDictionary(x => x, x => requestService.RequestAsync(x, xmlRequest, cancellationToken));

        await Task.WhenAll(platformRequests.Values);

        zones = [];

        foreach (var (platform, responseTask) in platformRequests)
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
