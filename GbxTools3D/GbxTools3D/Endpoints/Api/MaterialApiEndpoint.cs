using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Data.Entities;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Immutable;

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

        var materialDtos = materials
            .GroupBy(x => x.Name)
            .ToDictionary(x => x.Key, MapMaterialGrouping);

        return TypedResults.Ok(materialDtos);
    }

    private static MaterialDto MapMaterialGrouping(IGrouping<string, Material> material)
    {
        var materialDto = default(MaterialDto);
        var modifiers = default(ImmutableDictionary<string, MaterialDto>.Builder);

        foreach (var m in material)
        {
            if (m.Modifier is not null)
            {
                modifiers ??= ImmutableDictionary.CreateBuilder<string, MaterialDto>();
                modifiers[m.Modifier.Name] = MapMaterial(m);
                continue;
            }

            if (materialDto is not null)
            {
                throw new InvalidOperationException($"Multiple materials with the same name '{m.Name}' found without modifiers.");
            }

            materialDto = MapMaterial(m);
        }

        if (materialDto is null)
        {
            throw new InvalidOperationException($"No non-modifier material found for name '{material.Key}'.");
        }

        if (modifiers?.Count > 0)
        {
            materialDto.Modifiers = modifiers.ToImmutable();
        }

        return materialDto;
    }

    private static MaterialDto MapMaterial(Material material) => new()
    {
        SurfaceId = material.SurfaceId,
        IsShader = material.IsShader,
        Shader = material.Shader?.Name,
        Textures = material.Textures.Count == 0 ? null : material.Textures
    };
}