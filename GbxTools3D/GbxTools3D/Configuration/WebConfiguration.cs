using Blazored.LocalStorage;
using GbxTools3D.Client.Converters;
using GbxTools3D.Health;
using ManiaAPI.TMX.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.ResponseCompression;
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

        services.AddBlazoredLocalStorage();

        services.AddHttpClient("exchange", client =>
        {
            client.DefaultRequestHeaders.Add(Microsoft.Net.Http.Headers.HeaderNames.UserAgent, "GbxTools3D");
        }).AddStandardResilienceHandler();

        services.AddTMX();

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

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
}
