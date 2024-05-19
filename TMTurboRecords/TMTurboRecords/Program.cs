using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using TMTurboRecords;
using TMTurboRecords.Components;
using TMTurboRecords.Extensions;
using TMTurboRecords.Services;
using Blazored.LocalStorage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TMTR", Version = "v1" });
});

foreach (var platform in MasterServer.Platforms)
{
    for (var i = 1; i <= 3; i++)
    {
        var platformServer = $"{i:000}-{platform}";

        builder.Services.AddHttpClient(platformServer, client =>
        {
            client.BaseAddress = new Uri($"http://mp{platformServer}.turbo.trackmania.com/game/request.php");
        });
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddTransient<ZoneService>();
builder.Services.AddTransient<RecordService>();

builder.Services.AddEndpoints();
builder.Services.AddMudServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseSwagger();

app.MapScalarUi();

app.UseEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TMTurboRecords.Client._Imports).Assembly);

app.Run();
