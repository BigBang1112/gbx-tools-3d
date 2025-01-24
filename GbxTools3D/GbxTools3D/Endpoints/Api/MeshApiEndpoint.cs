using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Services;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class MeshApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetMeshInfo)
            .CacheOutput(x => x.Tag("mesh"));
        group.MapGet("/{hash}", GetMeshByHash); // cache output questionable due to larger memory usage
        group.MapGet("/{hash}/{lod}", GetMeshLodByHash); // cache output questionable due to larger memory usage
    }

    private static async Task<IResult> GetMeshInfo(MeshService meshService, CancellationToken cancellationToken)
    {
        var meshCount = await meshService.GetMeshCountAsync(cancellationToken);

        return Results.Ok(new MeshInfoDto
        {
            Count = meshCount
        });
    }

    private static async Task<IResult> GetMeshByHash(MeshService meshService, string hash, HttpContext context, CancellationToken cancellationToken)
    {
        var mesh = await meshService.GetMeshByHashHqAsync(hash, cancellationToken);

        if (mesh is null)
        {
            return Results.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return Results.File(mesh.Data, "application/octet-stream", lastModified: mesh.CreatedAt);
    }

    private static async Task<IResult> GetMeshLodByHash(AppDbContext db, string hash, int lod, CancellationToken cancellationToken)
    {
        if (lod is < 0 or > 2)
        {
            return Results.BadRequest("Invalid LOD");
        }

        var mesh = await db.Meshes.FirstOrDefaultAsync(x => x.Hash == hash, cancellationToken);

        if (mesh is null)
        {
            return Results.NotFound();
        }

        switch (lod)
        {
            case 2:
                if (mesh.DataELQ is not null)
                {
                    return Results.File(mesh.DataELQ, "application/octet-stream", lastModified: mesh.CreatedAt);
                }

                if (mesh.DataLQ is not null)
                {
                    return Results.Redirect($"/api/mesh/{hash}/1");
                }
                break;
            case 1:
                if (mesh.DataLQ is not null)
                {
                    return Results.File(mesh.DataLQ, "application/octet-stream", lastModified: mesh.CreatedAt);
                }
                break;
        }

        return Results.Redirect($"/api/mesh/{hash}", permanent: true);
    }
}
