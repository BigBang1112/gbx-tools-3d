using GbxTools3D.Client.Dtos;
using GbxTools3D.Data;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Endpoints.Api;

public static class MeshApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", GetMeshInfo)
            .CacheOutput(x => x.Tag("mesh"));
        group.MapGet("/{hash}", GetMeshByHash); // cache output questionable due to larger memory usage
    }

    private static async Task<Ok<MeshInfoDto>> GetMeshInfo(MeshService meshService, CancellationToken cancellationToken)
    {
        var meshCount = await meshService.GetMeshCountAsync(cancellationToken);

        return TypedResults.Ok(new MeshInfoDto
        {
            Count = meshCount
        });
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> GetMeshByHash(
        MeshService meshService, 
        string hash,  
        HttpContext context, 
        CancellationToken cancellationToken,
        [FromQuery] bool collision = false)
    {
        var mesh = collision
            ? await meshService.GetMeshSurfByHashAsync(hash, cancellationToken)
            : await meshService.GetMeshHqByHashAsync(hash, cancellationToken);

        if (mesh is null)
        {
            return TypedResults.NotFound();
        }

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.File(mesh.Data, "application/octet-stream", lastModified: mesh.CreatedAt);
    }

    private static async Task<Results<FileContentHttpResult, NotFound, BadRequest<string>, RedirectHttpResult>> GetMeshLodByHash(AppDbContext db, string hash, int lod, CancellationToken cancellationToken)
    {
        if (lod is < 0 or > 2)
        {
            return TypedResults.BadRequest("Invalid LOD");
        }

        var mesh = await db.Meshes.FirstOrDefaultAsync(x => x.Hash == hash, cancellationToken);

        if (mesh is null)
        {
            return TypedResults.NotFound();
        }

        switch (lod)
        {
            case 2:
                if (mesh.DataVLQ is not null)
                {
                    return TypedResults.File(mesh.DataVLQ, "application/octet-stream", lastModified: mesh.UpdatedAt);
                }

                if (mesh.DataLQ is not null)
                {
                    return TypedResults.Redirect($"/api/mesh/{hash}/1");
                }
                break;
            case 1:
                if (mesh.DataLQ is not null)
                {
                    return TypedResults.File(mesh.DataLQ, "application/octet-stream", lastModified: mesh.UpdatedAt);
                }
                break;
        }

        return TypedResults.Redirect($"/api/mesh/{hash}", permanent: true);
    }
}
