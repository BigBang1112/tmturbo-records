using ManiaAPI.Xml.TMT;

namespace TMTurboRecords;

public sealed class InitHostedService : IHostedService
{
    private readonly IServiceScopeFactory scopeFactory;

    public InitHostedService(IServiceScopeFactory scopeFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        foreach (var platform in Enum.GetValues<Platform>())
        {
            var initServer = scope.ServiceProvider.GetRequiredKeyedService<InitServerTMT>(platform);
            var waitingParams = await initServer.GetWaitingParamsAsync();
            var masterServer = scope.ServiceProvider.GetRequiredKeyedService<MasterServerTMT>(platform);
            masterServer.Client.BaseAddress = waitingParams.MasterServers.First().GetUri();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
