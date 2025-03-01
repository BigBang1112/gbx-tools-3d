using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class CollectionApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}", GetCollections)
            .CacheOutput(x => x.Tag("collection"));
    }

    private static async Task<Ok<List<CollectionDto>>> GetCollections(
        ICollectionClientService collectionClientService,
        GameVersion gameVersion,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var collections = await collectionClientService.GetAllAsync(gameVersion, cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(collections);
    }
}