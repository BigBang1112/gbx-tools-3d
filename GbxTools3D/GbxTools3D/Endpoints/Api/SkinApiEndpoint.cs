using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class SkinApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/mp/{id}", GetSkinFromManiaPark)
            .RequireRateLimiting("fixed-external-downloads");
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> GetSkinFromManiaPark(
        HttpContext context,
        IHttpClientFactory httpFactory,
        string id,
        CancellationToken cancellationToken)
    {
        var http = httpFactory.CreateClient("exchange");
        
        using var skinResponse = await http.GetAsync($"https://maniapark.com/skin/{id}/download", cancellationToken);
        
        if (skinResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return TypedResults.NotFound();
        }

        skinResponse.EnsureSuccessStatusCode();

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.File(await skinResponse.Content.ReadAsStreamAsync(cancellationToken),
            "application/zip",
            skinResponse.Content.Headers.ContentDisposition?.FileName,
            skinResponse.Content.Headers.LastModified);
    }
}
