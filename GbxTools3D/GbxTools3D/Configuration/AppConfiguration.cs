using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using GbxTools3D.Client.Converters;
using GbxTools3D.Client.Services;
using GbxTools3D.Components;
using GbxTools3D.Data;
using GbxTools3D.Endpoints;
using GbxTools3D.Health;
using GbxTools3D.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace GbxTools3D.Configuration;

internal static class AppConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<PopulateDbService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<CollectionService>();
        services.AddScoped<ICollectionClientService, CollectionClientService>();
        services.AddScoped<IBlockClientService, BlockClientService>();
        services.AddScoped<IDecorationClientService, DecorationClientService>();
        services.AddScoped<IVehicleClientService, VehicleClientService>();
        services.AddScoped<MaterialService>();
        services.AddScoped<MeshService>();

        // client-sided services
        services.AddSingleton<GbxService>();
    }
    
    public static void AddDataServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            var connectionStr = config.GetConnectionString("DefaultConnection");
            options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr))
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.CommandExecuted)); // should be configurable
            //options.UseInMemoryDatabase("GbxTools3D");
        });
    }
    
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

    public static void AddTelemetryServices(this IServiceCollection services, IConfiguration config, IHostEnvironment environment)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
            .WriteTo.OpenTelemetry(config["OTEL_EXPORTER_OTLP_ENDPOINT"] + "/v1/logs", Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf)
            .CreateLogger();

        services.AddSerilog();

        services.AddOpenTelemetry()
            .WithMetrics(options =>
            {
                options
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });

                options.AddMeter("System.Net.Http");
            })
            .WithTracing(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.SetSampler<AlwaysOnSampler>();
                }

                options
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddOtlpExporter(options =>
                    {
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                    });
            });
        services.AddMetrics();
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
            .AddAdditionalAssemblies(typeof(GbxTools3D.Client._Imports).Assembly);
    }
    
    public static void MigrateDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.IsRelational())
        {
            dbContext.Database.Migrate();
        }
    }
}