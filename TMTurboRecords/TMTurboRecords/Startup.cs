using TMTurboRecords.Services;

namespace TMTurboRecords;

public sealed class Startup : IHostedService
{
    private readonly RequestService requestService;

    public Startup(RequestService requestService)
    {
        this.requestService = requestService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await requestService.InitAllAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
