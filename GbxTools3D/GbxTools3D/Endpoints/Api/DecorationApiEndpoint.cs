using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class DecorationApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}/{collectionName}", GetDecorations)
            .CacheOutput(x => x.Tag("decoration"));
    }

    private static async Task<Results<Ok<IEnumerable<DecorationSizeDto>>, NotFound, StatusCodeHttpResult>> GetDecorations(
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
        
        var decorations = await db.Decorations
            .Include(x => x.DecorationSize)
                .ThenInclude(x => x.Collection)
            .Where(x => x.DecorationSize.Collection == collection)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(MapDecorations(decorations));
    }

    private static IEnumerable<DecorationSizeDto> MapDecorations(IEnumerable<Decoration> decos) =>
        decos.GroupBy(x => new Int3(x.DecorationSize.SizeX, x.DecorationSize.SizeY, x.DecorationSize.SizeZ))
            .Select(decoGroup => new DecorationSizeDto
            {
                Size = decoGroup.Key,
                BaseHeight = decoGroup.First().DecorationSize.BaseHeight,
                Decorations = decoGroup.Select(x => new DecorationDto
                {
                    Name = x.Name,
                    Musics = x.Musics,
                    Sounds = x.Sounds,
                    Remap = x.Remap
                }).ToList(),
                Scene = decoGroup.First().DecorationSize.Scene
            });
}