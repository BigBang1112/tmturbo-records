using ManiaAPI.Xml.TMT;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TMTurboRecords.Health;

public class InitServerHealthCheck(HttpClient http) : IHealthCheck
{
    private readonly HttpClient http = http;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        await foreach (var (platform, task) in Enum.GetValues<Platform>()
            .ToDictionary(x => http.GetAsync(InitServerTMT.GetDefaultAddress(x), cancellationToken))
            .WhenEach())
        {
            try
            {
                var response = await task;
                data[platform.ToString()] = response.StatusCode;
            }
            catch (OperationCanceledException)
            {
                data[platform.ToString()] = "Timeout";
            }
            catch (Exception ex)
            {
                data[platform.ToString()] = ex.Message;
            }
        }

        if (data.Values.All(x => x is System.Net.HttpStatusCode.OK))
        {
            return HealthCheckResult.Healthy(data: data);
        }

        if (data.Values.Any(x => x is System.Net.HttpStatusCode.OK))
        {
            return HealthCheckResult.Degraded(data: data);
        }

        return HealthCheckResult.Unhealthy(data: data);
    }
}