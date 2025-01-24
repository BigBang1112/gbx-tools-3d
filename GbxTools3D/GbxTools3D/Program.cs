using GbxTools3D.Components;
using GbxTools3D.Data;
using GbxTools3D.Endpoints;
using GbxTools3D.Health;
using GbxTools3D.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient("exchange", client =>
{
    client.DefaultRequestHeaders.Add(Microsoft.Net.Http.Headers.HeaderNames.UserAgent, "GbxTools3D");
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddOutputCache();
#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

builder.Services.AddRateLimiter(options =>
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

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    //var connectionStr = builder.Configuration.GetConnectionString("DefaultConnection");
    //options.UseMySql(connectionStr, ServerVersion.AutoDetect(connectionStr));
    options.UseInMemoryDatabase("GbxTools3D");
});

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
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

builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken();
builder.Services.AddAuthorization();

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Sixteen, applyThemeToRedirectedOutput: true)
    .WriteTo.OpenTelemetry()
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddOpenTelemetry()
    .WithMetrics(options =>
    {
        options
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter();

        options.AddMeter("System.Net.Http");
    })
    .WithTracing(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.SetSampler<AlwaysOnSampler>();
        }

        options
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter();
    });
builder.Services.AddMetrics();

builder.Services.AddHostedService<PopulateDbService>();
builder.Services.AddScoped<CollectionService>();
builder.Services.AddScoped<MeshService>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.IsRelational())
    {
        dbContext.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();

    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
        options.Theme = ScalarTheme.DeepSpace;
    });
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.MapHealthChecks("/_health", new()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
    .RequireAuthorization();

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

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(GbxTools3D.Client._Imports).Assembly);

app.Run();
