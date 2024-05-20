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

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });
}

builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TMTR", Version = "v1" });
});

foreach (var platform in Enum.GetValues<Platform>().Skip(1).Select(x => x.GetInitClientName()))
{
    builder.Services.AddHttpClient(platform, client =>
    {
        client.BaseAddress = new Uri($"http://{platform}.turbo.trackmania.com/game/request.php");
    });
}

builder.Services.AddHttpClient("relay", client =>
{
    client.DefaultRequestHeaders.AcceptEncoding.Add(new("gzip"));
})
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
    });

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddHostedService<Startup>();

builder.Services.AddTransient<RequestService>();
builder.Services.AddTransient<ZoneService>();
builder.Services.AddTransient<RecordService>();

builder.Services.AddEndpoints();
builder.Services.AddMudServices();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonTimeInt32Converter());
});

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

if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

app.UseStaticFiles();

app.UseAntiforgery();

app.UseSwagger();

app.MapScalarUi();

app.UseEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(TMTurboRecords.Client._Imports).Assembly);

app.Run();
