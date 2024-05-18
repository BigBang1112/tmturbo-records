namespace TMTurboRecords.Client;

public sealed class HttpService
{
    public Task<HttpResponseMessage>? ResponseTask { get; set; }
    public event Func<HttpResponseMessage, Task>? ResponseReceived;

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (ResponseTask?.IsCompleted == true)
            {
                var response = await ResponseTask;
                if (response.IsSuccessStatusCode && ResponseReceived is not null)
                {
                    await ResponseReceived.Invoke(response);
                }
                response.Dispose();
                ResponseTask = null;
            }

            await Task.Delay(50, stoppingToken);
        }
    }
}
