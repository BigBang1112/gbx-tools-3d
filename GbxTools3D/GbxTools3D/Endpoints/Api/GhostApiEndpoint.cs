using Microsoft.AspNetCore.Http.HttpResults;
using System.Web;

namespace GbxTools3D.Endpoints.Api;

public static class GhostApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/wrr/{mapUid}/{time}/{login}", GetGhostFromWorldRecordReport)
            .RequireRateLimiting("fixed-external-downloads");
        group.MapGet("/tmt/{url}", GetGhostTMTurbo)
            .RequireRateLimiting("fixed-external-downloads");
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, BadRequest>> GetGhostFromWorldRecordReport(
        HttpContext context,
        IHttpClientFactory httpFactory,
        string mapUid,
        int time,
        string login,
        CancellationToken cancellationToken)
    {
        var http = httpFactory.CreateClient("wrr");

        var ghostResponse = await http.GetAsync($"https://wr.bigbang1112.cz/api/v1/ghost/download/{mapUid}/{time}/{login}", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (ghostResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ghostResponse.Dispose();
            return TypedResults.NotFound();
        }

        ghostResponse.EnsureSuccessStatusCode();

        context.Response.Headers.CacheControl = "max-age=3600";
        context.Response.RegisterForDispose(ghostResponse);

        var stream = await ghostResponse.Content.ReadAsStreamAsync(cancellationToken);

        return TypedResults.File(
            stream,
            "application/gbx",
            ghostResponse.Content.Headers.ContentDisposition?.FileName,
            ghostResponse.Content.Headers.LastModified,
            ghostResponse.Headers.ETag is null
                ? null
                : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(ghostResponse.Headers.ETag.Tag, ghostResponse.Headers.ETag.IsWeak)
        );
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, BadRequest>> GetGhostTMTurbo(
        HttpContext context,
        IHttpClientFactory httpFactory,
        string url,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(HttpUtility.UrlDecode(url), UriKind.Absolute, out var uri) || uri.Scheme != "https")
        {
            return TypedResults.BadRequest();
        }

        if (!uri.Host.EndsWith(".turbo.trackmania.com") || !uri.LocalPath.StartsWith("/data/official_replays/records/"))
        {
            return TypedResults.BadRequest();
        }

        var http = httpFactory.CreateClient("tmt");

        var ghostResponse = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (ghostResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            ghostResponse.Dispose();
            return TypedResults.NotFound();
        }

        ghostResponse.EnsureSuccessStatusCode();

        context.Response.Headers.CacheControl = "max-age=3600";
        context.Response.RegisterForDispose(ghostResponse);

        var stream = await ghostResponse.Content.ReadAsStreamAsync(cancellationToken);

        return TypedResults.File(
            stream,
            "application/gbx",
            ghostResponse.Content.Headers.ContentDisposition?.FileName,
            ghostResponse.Content.Headers.LastModified,
            ghostResponse.Headers.ETag is null
                ? null
                : new Microsoft.Net.Http.Headers.EntityTagHeaderValue(ghostResponse.Headers.ETag.Tag, ghostResponse.Headers.ETag.IsWeak)
        );
    }
}
