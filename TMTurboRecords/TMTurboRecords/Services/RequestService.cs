using LazyCache;
using System.Text;
using System.Xml.Linq;
using TMTurboRecords.Extensions;
using TMTurboRecords.Shared.Models;

namespace TMTurboRecords.Services;

public sealed class RequestService
{
    private readonly IHttpClientFactory httpFactory;
    private readonly IAppCache cache;
    private readonly ILogger<RequestService> logger;

    public RequestService(IHttpClientFactory httpFactory, IAppCache cache, ILogger<RequestService> logger)
    {
        this.httpFactory = httpFactory;
        this.cache = cache;
        this.logger = logger;
    }

    public async Task InitAllAsync(CancellationToken cancellationToken)
    {
        var tasks = PlatformExtensions.Platforms
            .ToDictionary(x => x, x => InitAsync(x, cancellationToken));

        await Task.WhenAll(tasks.Values);

        foreach (var (platform, task) in tasks)
        {
            if (task.Result is not null)
            {
                cache.Add($"RelayUrl_{platform}", task.Result, TimeSpan.FromDays(1));
            }
        }
    }

    public async Task<string?> InitAsync(Platform platform, CancellationToken cancellationToken)
    {
        var initClient = platform.GetInitClientName();

        var relayClient = httpFactory.CreateClient(initClient);

        const string xmlRequest = @"<root>
    <game>
        <name>ManiaPlanet</name>
        <version>3.3.0</version>
        <build>2016-11-07_16_15</build>
    </game>
    <request>
        <name>GetWaitingParams</name>
    </request>
</root>";

        using var response = await relayClient.SendAsync(new HttpRequestMessage(
            HttpMethod.Post,
            $"http://{initClient}.turbo.trackmania.com/game/request.php")
        {
            Content = new StringContent(xmlRequest)
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

            return null;
        }

        var ms = contentElement.Element("ms");

        if (ms is null)
        {
            logger.LogError("XML-RPC error: Missing master server info");
            return null;
        }

        var name = ms.Element("b")?.Value;
        var url = ms.Element("c")?.Value;

        if (string.IsNullOrEmpty(url))
        {
            logger.LogError("XML-RPC error: Missing master server URL");
            return null;
        }

        logger.LogInformation("Initialized {Platform} relay client, URL: {Url}, name: {Name}", platform, url, name);

        return url;
    }

    public async Task<HttpResponseMessage> RequestAsync(Platform platform, string xmlContent, CancellationToken cancellationToken)
    {
        var relayUrl = await cache.GetOrAddAsync($"RelayUrl_{platform}", async entry =>
        {
            var url = await InitAsync(platform, cancellationToken) ?? throw new InvalidOperationException("Failed to initialize relay client");
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            return url;
        });

        var relayClient = httpFactory.CreateClient($"relay-{platform}");

        return await relayClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"https://{relayUrl}/game/request.php")
        {
            Content = new StringContent(xmlContent, Encoding.UTF8, "application/xml")
        }, cancellationToken);
    }
}
