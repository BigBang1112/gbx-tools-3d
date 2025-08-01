﻿using GbxTools3D.Components;
using GbxTools3D.Endpoints;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.CookiePolicy;
using Scalar.AspNetCore;

namespace GbxTools3D.Configuration;

public static class MiddlewareConfiguration
{
    public static void UseMiddleware(this WebApplication app)
    {
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
            .AddInteractiveServerRenderMode(x => x.ContentSecurityFrameAncestorsPolicy = "*") // for iframes on other sites
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);
    }
}
