using GBX.NET;
using GbxTools3D.Data;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class DecorationApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}/{collectionName}", GetDecorationInfo);
    }

    private static async Task<IResult> GetDecorationInfo(
        AppDbContext db, 
        GameVersion gameVersion, 
        string collectionName, 
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var collection = await db.Collections
            .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == collectionName, cancellationToken);

        if (collection is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NotFound();
    }
}