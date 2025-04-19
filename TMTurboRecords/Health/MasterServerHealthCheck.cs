using ManiaAPI.Xml.TMT;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TMTurboRecords.Health;

public class MasterServerHealthCheck(HttpClient http) : IHealthCheck
{
    private readonly HttpClient http = http;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        await foreach (var (subDomain, task) in Enum.GetValues<Platform>()
            .SelectMany(platform => Enumerable.Range(1, 3).Select(i => $"mp{i:D3}-{platform.ToString().ToLowerInvariant()}"))
            .ToDictionary(x => x, subDomain => http.GetAsync($"https://{subDomain}.turbo.trackmania.com/game/request.php", cancellationToken))
            .WhenEach())
        {
            try
            {
                var response = await task;
                data[subDomain] = response.StatusCode;
            }
            catch (OperationCanceledException)
            {
                data[subDomain] = "Timeout";
            }
            catch (Exception ex)
            {
                data[subDomain] = ex.Message;
            }
        }

        // if all platforms of a particular server number are available, then its healthy
        var serverNumbers = data.Keys
            .Select(x => x.Split('-')[0])
            .Distinct()
            .ToList();

        foreach (var serverNumber in data.Keys.Select(x => x[..5]))
        {
            var serverData = data
                .Where(x => x.Key.StartsWith(serverNumber))
                .Select(x => x.Value)
                .ToList();

            if (serverData.All(x => x is System.Net.HttpStatusCode.OK))
            {
                return HealthCheckResult.Healthy(data: data);
            }

            if (serverData.Any(x => x is System.Net.HttpStatusCode.OK))
            {
                return HealthCheckResult.Degraded(data: data);
            }
        }

        return HealthCheckResult.Unhealthy(data: data);
    }
}