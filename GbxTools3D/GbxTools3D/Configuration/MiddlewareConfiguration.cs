using GbxTools3D.Components;
using GbxTools3D.Endpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.CookiePolicy;
using Scalar.AspNetCore;

namespace GbxTools3D.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseResponseCompression();
        }

        app.UseCookiePolicy(new CookiePolicyOptions
        {
            MinimumSameSitePolicy = SameSiteMode.Lax,
            Secure = CookieSecurePolicy.Always,
            HttpOnly = HttpOnlyPolicy.Always
        });

        app.UseRateLimiter();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.UseOutputCache();

        app.MapHealthChecks("/_health", new()
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).RequireAuthorization();

        app.MapStaticAssets();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
                options.Theme = ScalarTheme.DeepSpace;
            });
        }

        app.MapEndpoints();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
