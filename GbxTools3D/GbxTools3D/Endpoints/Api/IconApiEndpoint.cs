using GBX.NET;
using GbxTools3D.Data;
using GbxTools3D.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class IconApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        // cache output questionable due to larger memory usage
        group.MapGet("/{gameVersion}/collection/{collectionName}/environment", GetEnvironmentIconByCollectionName);
        group.MapGet("/{gameVersion}/collection/{collectionName}/small", GetSmallIconByCollectionName);
        group.MapGet("/{gameVersion}/collection/{collectionName}/block/{blockName}", GetIconByBlockName);
        //group.MapGet("/{gameVersion}/collection/{collectionName}/decoration/{decorationName}", GetIconByDecorationName);
        group.MapGet("/{gameVersion}/vehicle/{vehicleName}", GetVehicleIconByVehicleName);
    }

    private static readonly Func<AppDbContext, GameVersion, string, Task<CacheableData?>> IconByCollectionNameAsync = EF.CompileAsyncQuery((
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName)
            => db.Collections
                .Where(x => x.GameVersion == gameVersion && x.Name == collectionName && x.Icon!.Data != null)
                .Select(x => new CacheableData { Data = x.Icon!.Data!, UpdatedAt = x.Icon!.UpdatedAt })
                .FirstOrDefault());

    private static readonly Func<AppDbContext, GameVersion, string, Task<CacheableData?>> IconSmallByCollectionNameAsync = EF.CompileAsyncQuery((
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName)
            => db.Collections
                .Where(x => x.GameVersion == gameVersion && x.Name == collectionName && x.IconSmall!.Data != null)
                .Select(x => new CacheableData { Data = x.IconSmall!.Data!, UpdatedAt = x.IconSmall!.UpdatedAt })
                .FirstOrDefault());

    private static readonly Func<AppDbContext, GameVersion, string, string, Task<CacheableData?>> IconByBlockNameAsync = EF.CompileAsyncQuery((
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        string blockName)
            => db.BlockInfos
                .Where(x => x.Collection.GameVersion == gameVersion && x.Collection.Name == collectionName && x.Name == blockName && x.Icon!.Data != null)
                .Select(x => new CacheableData { Data = x.Icon!.Data!, UpdatedAt = x.Icon!.UpdatedAt })
                .FirstOrDefault());

    private static readonly Func<AppDbContext, GameVersion, string, Task<CacheableData?>> IconByVehicleNameAsync = EF.CompileAsyncQuery((
        AppDbContext db,
        GameVersion gameVersion,
        string vehicleName)
            => db.Vehicles
                .Where(x => x.GameVersion == gameVersion && x.Name == vehicleName && x.Icon!.Data != null)
                .Select(x => new CacheableData { Data = x.Icon!.Data!, UpdatedAt = x.Icon!.UpdatedAt })
                .FirstOrDefault());

    private static async Task<Results<FileContentHttpResult, NotFound>> GetEnvironmentIconByCollectionName(
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var icon = await IconByCollectionNameAsync(db, gameVersion, collectionName);

        if (icon is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";
        return TypedResults.File(icon.Data, "image/webp", $"{collectionName}.webp", lastModified: icon.UpdatedAt);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetSmallIconByCollectionName(
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var icon = await IconSmallByCollectionNameAsync(db, gameVersion, collectionName);
        
        if (icon is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";
        return TypedResults.File(icon.Data, "image/webp", $"{collectionName}.webp", lastModified: icon.UpdatedAt);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetIconByBlockName(
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        string blockName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var icon = await IconByBlockNameAsync(db, gameVersion, collectionName, blockName);

        if (icon is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.File(icon.Data, "image/webp", $"{blockName}.webp", lastModified: icon.UpdatedAt);
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetIconByDecorationName(
        AppDbContext db,
        GameVersion gameVersion,
        string collectionName,
        string decorationName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        /*var icon = await IconByBlockNameAsync(db, gameVersion, collectionName, decorationName);
        if (icon is null)
        {
            return TypedResults.NotFound();
        }
        context.Response.Headers.CacheControl = "max-age=3600";
        return TypedResults.File(icon.Data, "image/webp", $"{decorationName}.webp", lastModified: icon.UpdatedAt);*/
        return TypedResults.NotFound();
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetVehicleIconByVehicleName(
        AppDbContext db,
        GameVersion gameVersion,
        string vehicleName,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var icon = await IconByVehicleNameAsync(db, gameVersion, vehicleName);

        if (icon is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.File(icon.Data, "image/webp", $"{vehicleName}.webp", lastModified: icon.UpdatedAt);
    }
}
