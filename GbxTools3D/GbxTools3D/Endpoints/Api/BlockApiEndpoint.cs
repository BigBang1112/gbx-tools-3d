using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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

    private static async Task<Results<Ok<IEnumerable<BlockInfoDto>>, NotFound, StatusCodeHttpResult>> GetBlockInfos(
        AppDbContext db, 
        GameVersion gameVersion, 
        string collectionName, 
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == collectionName, cancellationToken);

        if (collection is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.GetTypedHeaders().LastModified = collection.UpdatedAt;

        if (context.Request.GetTypedHeaders().IfModifiedSince?.UtcDateTime == collection.UpdatedAt)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
        
        var blockInfos = await db.BlockInfos
            .Include(x => x.Collection)
            .Include(x => x.Variants)
            .Where(x => x.Collection == collection)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(blockInfos.Select(MapBlockInfo));
    }

    private static async Task<Results<Ok<BlockInfoDto>, NotFound, StatusCodeHttpResult>> GetBlockInfoByName(
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        string blockName, 
        HttpContext context, 
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == collectionName, cancellationToken);

        if (collection is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.GetTypedHeaders().LastModified = collection.UpdatedAt;

        if (context.Request.GetTypedHeaders().IfModifiedSince?.UtcDateTime == collection.UpdatedAt)
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }
        
        var blockInfo = await db.BlockInfos
            .Include(x => x.Collection)
            .Include(x => x.Variants)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Collection == collection && x.Name == blockName, cancellationToken);

        if (blockInfo is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(MapBlockInfo(blockInfo));
    }

    private static BlockInfoDto MapBlockInfo(BlockInfo blockInfo) => new()
    {
        Name = blockInfo.Name,
        Collection = blockInfo.Collection.Name,
        AirUnits = blockInfo.AirUnits,
        GroundUnits = blockInfo.GroundUnits,
        HasAirHelper = blockInfo.HasAirHelper,
        HasGroundHelper = blockInfo.HasGroundHelper,
        HasConstructionModeHelper = blockInfo.HasConstructionModeHelper,
        AirVariants = blockInfo.Variants.Where(x => !x.Ground).Select(MapVariant).ToList(),
        GroundVariants = blockInfo.Variants.Where(x => x.Ground).Select(MapVariant).ToList(),
        Height = blockInfo.Height,
    };

    private static BlockVariantDto MapVariant(BlockVariant variant) => new()
    {
        Variant = variant.Variant,
        SubVariant = variant.SubVariant,
        ObjectLinks = variant.ObjectLinks.Count == 0 ? null : variant.ObjectLinks.Select(x => new ObjectLinkDto
        {
            Location = new Iso4(x.XX, x.XY, x.XZ, x.YX, x.YY, x.YZ, x.ZX, x.ZY, x.ZZ, x.TX, x.TY, x.TZ),
        }).ToList()
    };
}