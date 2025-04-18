using GbxTools3D.Client.Converters;
using GbxTools3D.Components;
using GbxTools3D.Endpoints;
using GbxTools3D.Health;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace GbxTools3D.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddHttpClient("exchange", client =>
        {
            client.DefaultRequestHeaders.Add(Microsoft.Net.Http.Headers.HeaderNames.UserAgent, "GbxTools3D");
        }).AddStandardResilienceHandler();

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

            options.AddPolicy("fixed-external-downloads", context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey: context.Connection.RemoteIpAddress?.ToString(), factory =>
                {
                    return new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1)
                    };
                });
            });
        });

        services.AddOpenApi();

        services.AddHealthChecks()
            //.AddMySql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");
            .AddCheck<TmxTMUFHealthCheck>("tmx-tmuf")
            .AddCheck<TmxTMNFHealthCheck>("tmx-tmnf")
            .AddCheck<TmxNationsHealthCheck>("tmx-nations")
            .AddCheck<TmxSunriseHealthCheck>("tmx-sunrise")
            .AddCheck<TmxOriginalHealthCheck>("tmx-original")
            .AddCheck<MxTM2HealthCheck>("mx-tm2")
            .AddCheck<MxSMHealthCheck>("mx-sm")
            .AddCheck<MxTM2020HealthCheck>("mx-tm2020")
            .AddCheck<TmIoHealthCheck>("tmio");

        services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
            .AddBearerToken();
        services.AddAuthorization();

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            options.SerializerOptions.Converters.Add(new JsonInt3Converter());
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

        app.MapEndpoints();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }

    public static void UseSecurityMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                options.Theme = ScalarTheme.DeepSpace;
            });
        }

        app.UseRateLimiter();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

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
