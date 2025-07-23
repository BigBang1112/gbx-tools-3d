using Blazored.LocalStorage;
using GbxTools3D.Client.Converters;
using GbxTools3D.Health;
using ManiaAPI.TMX.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.ResponseCompression;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ManiaAPI.ManiaPlanetAPI.Extensions.Hosting;
using ManiaAPI.ManiaPlanetAPI;

namespace GbxTools3D.Configuration;

public static class WebConfiguration
{
    public static void AddWebServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        services.AddRazorComponents(options =>
        {
            options.DetailedErrors = environment.IsDevelopment();
        })
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        services.AddBlazoredLocalStorage();

        services.AddHttpClient("exchange", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent("Exchange"));
        }).AddStandardResilienceHandler();

        services.AddHttpClient("wrr", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent("WRR"));
        }).AddStandardResilienceHandler();

        services.AddHttpClient("maniaplanet", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent("ManiaPlanet"));
        }).AddStandardResilienceHandler();

        services.AddTMX();

        services.AddManiaPlanetAPI(options =>
        {
            var clientId = config["ManiaPlanetAPI:ClientId"] ?? throw new Exception("ManiaPlanetAPI:ClientId is not configured.");
            var clientSecret = config["ManiaPlanetAPI:ClientSecret"] ?? throw new Exception("ManiaPlanetAPI:ClientSecret is not configured.");

            options.Credentials = new ManiaPlanetAPICredentials(clientId, clientSecret);
        })
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent("ManiaPlanet ManiaAPI"));
            })
            .AddStandardResilienceHandler();

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

    private static string GetUserAgent(string clientName)
    {
        return $"GbxTools3D/1.0 ({clientName} Client; Email=petrpiv1@gmail.com; Discord=bigbang1112)";
    }
}
