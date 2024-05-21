using Microsoft.OpenApi.Models;
using MudBlazor.Services;
using TMTurboRecords;
using TMTurboRecords.Components;
using TMTurboRecords.Extensions;
using TMTurboRecords.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Http.Json;
using TMTurboRecords.Shared;
using System.Net;
using TMTurboRecords.Shared.Models;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddLazyCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TMTR", Version = "v1" });
});

foreach (var platform in Enum.GetValues<Platform>().Skip(1))
{
    builder.Services.AddHttpClient(platform.GetInitClientName(), client =>
    {
        client.BaseAddress = new Uri($"http://{platform}.turbo.trackmania.com/game/request.php");
    });

    builder.Services.AddHttpClient($"relay-{platform}", client =>
    {
        client.DefaultRequestHeaders.AcceptEncoding.Add(new("gzip"));
        client.Timeout = TimeSpan.FromSeconds(10);
    })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
        });
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddHostedService<Startup>();

builder.Services.AddSingleton<RequestService>();
builder.Services.AddTransient<ZoneService>();
builder.Services.AddTransient<RecordService>();

builder.Services.AddEndpoints();
builder.Services.AddMudServices();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonTimeInt32Converter());
});

var app = builder.Build();

app.UseForwardedHeaders();

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

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseStaticFiles();

app.UseSwagger();

app.MapScalarUi();

app.UseEndpoints();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TMTurboRecords.Client._Imports).Assembly);

app.Run();
