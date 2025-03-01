using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class BlockApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}/{collectionName}", GetBlockInfos)
            .CacheOutput(x => x.Tag("block"));
        group.MapGet("/{gameVersion}/{collectionName}/{blockName}", GetBlockInfoByName)
            .CacheOutput(x => x.Tag("block"));
    }

    private static async Task<Results<Ok<List<BlockInfoDto>>, NotFound, StatusCodeHttpResult>> GetBlockInfos(
        GameVersion gameVersion, 
        string collectionName,
        CollectionService collectionService,
        IBlockClientService blockClientService,
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
        
        var blockInfos = await blockClientService.GetAllAsync(gameVersion, collectionName, cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(blockInfos);
    }

    private static async Task<Results<Ok<BlockInfoDto>, NotFound, StatusCodeHttpResult>> GetBlockInfoByName(
        GameVersion gameVersion,
        string collectionName,
        string blockName,
        CollectionService collectionService,
        IBlockClientService blockClientService,
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

        var blockInfo = await blockClientService.GetAsync(gameVersion, collectionName, blockName, cancellationToken);

        if (blockInfo is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(blockInfo);
    }
}