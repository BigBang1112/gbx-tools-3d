using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class DecorationApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}/{collectionName}", GetDecorations)
            .CacheOutput(x => x.Tag("decoration"));
    }

    private static async Task<Results<Ok<List<DecorationSizeDto>>, NotFound, StatusCodeHttpResult>> GetDecorations(
        GameVersion gameVersion, 
        string collectionName,
        CollectionService collectionService,
        IDecorationClientService decorationClientService,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var collection = await collectionService.GetAsync(gameVersion, collectionName, cancellationToken);

        if (collection is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.GetTypedHeaders().LastModified = collection.UpdatedAt;

        if (context.Request.GetTypedHeaders().IfModifiedSince?.UtcDateTime == collection.UpdatedAt)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var decorations = await decorationClientService.GetAllAsync(gameVersion, collectionName, cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(decorations);
    }
}