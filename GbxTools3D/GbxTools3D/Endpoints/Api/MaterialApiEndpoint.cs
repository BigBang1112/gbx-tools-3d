using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Data.Entities;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class MaterialApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}", GetMaterials)
            .CacheOutput(x => x.Tag("material"));
    }

    private static async Task<Results<Ok<Dictionary<string, MaterialDto>>, NotFound, StatusCodeHttpResult>> GetMaterials(
        MaterialService materialService, 
        GameVersion gameVersion, 
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var materials = await materialService.GetAllAsync(gameVersion, cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(materials.ToDictionary(x => x.Name, MapMaterial));
    }

    private static MaterialDto MapMaterial(Material material) => new()
    {
        SurfaceId = material.SurfaceId,
        IsShader = material.IsShader,
        Shader = material.Shader?.Name,
        Textures = material.Textures.Count == 0 ? null : material.Textures
    };
}