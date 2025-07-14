using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class SkinApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/mp/{id}", GetSkinFromManiaPark)
            .RequireRateLimiting("fixed-external-downloads");
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, BadRequest>> GetSkinFromManiaPark(
        HttpContext context,
        IHttpClientFactory httpFactory,
        string id,
        CancellationToken cancellationToken)
    {
        var http = httpFactory.CreateClient("exchange");

        var skinResponse = await http.GetAsync($"https://maniapark.com/skin/{id}/download", HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (skinResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            skinResponse.Dispose();
            return TypedResults.NotFound();
        }

        skinResponse.EnsureSuccessStatusCode();

        if (skinResponse.Content.Headers.ContentType?.MediaType != "application/x-zip-compressed")
        {
            skinResponse.Dispose();
            return TypedResults.BadRequest();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        var stream = await skinResponse.Content.ReadAsStreamAsync(cancellationToken);

        return TypedResults.File(
            stream,
            "application/zip",
            skinResponse.Content.Headers.ContentDisposition?.FileName,
            skinResponse.Content.Headers.LastModified
        );
    }
}
