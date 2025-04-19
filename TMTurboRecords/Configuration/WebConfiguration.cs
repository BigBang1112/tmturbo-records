using HealthChecks.UI.Client;
using ManiaAPI.Xml.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using TMTurboRecords.Components;
using TMTurboRecords.Health;

namespace TMTurboRecords.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddHttpClient();

        services.AddMasterServerTMT();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.AddOutputCache();
        services.AddHybridCache();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
        });

        services.AddOpenApi();

        services.AddHealthChecks()
            .AddCheck<InitServerHealthCheck>("init-server")
            .AddCheck<MasterServerHealthCheck>("master-server", timeout: TimeSpan.FromSeconds(5));

        services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddBearerToken();
        services.AddAuthorization();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
    }

    public static void UseAuthMiddleware(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
    }

    public static void UseEndpointMiddleware(this WebApplication app)
    {
        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }

    public static void UseSecurityMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Theme = ScalarTheme.DeepSpace;
        });

        app.UseRateLimiter();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.UseOutputCache();

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax,
            Secure = CookieSecurePolicy.Always,
            HttpOnly = HttpOnlyPolicy.Always
        });

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }
    }
}