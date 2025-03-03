using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Extensions;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal sealed partial class Material
{
    private sealed record Properties(
        bool DoubleSided = false,
        bool WorldUV = false, 
        bool Transparent = false)
    {
        public static readonly Properties Default = new();
    }

    private static readonly Dictionary<string, Properties> shaderProperties = new()
    {
        ["Techno2/Media/Material/PDiff PDiff PA PX2 Grass2"] = new(WorldUV: true)
    };

    private static readonly Dictionary<string, JSObject> textures = [];
    private static readonly Dictionary<string, JSObject> materials = [];
    
    [JSImport("createRandomMaterial", nameof(Material))]
    private static partial JSObject CreateRandomMaterial();
    
    [JSImport("createMaterial", nameof(Material))]
    private static partial JSObject CreateMaterial(JSObject? diffuseTexture, JSObject? normalTexture, [JSMarshalAs<JSType.Any>] object properties);
    
    [JSImport("createTexture", nameof(Material))]
    private static partial JSObject CreateTexture(string path);
    
    private static JSObject? GetOrCreateTexture(string? path)
    {
        if (path is null)
        {
            return null;
        }

        if (textures.TryGetValue(path, out var texture))
        {
            return texture;
        }
        
        var hash = $"GbxTools3D|Texture|TMF|{path}|PeopleOnTheBusLikeDMCA".Hash();
        texture = CreateTexture($"api/texture/{hash}");
        textures.Add(path, texture);
        
        return texture;
    }
    
    public static JSObject GetOrCreateMaterial(string name, Dictionary<string, MaterialDto>? availableMaterials)
    {
        if (materials.TryGetValue(name, out var material))
        {
            return material;
        }

        if (availableMaterials is null || !availableMaterials.TryGetValue(name, out var materialDto))
        {
            material = CreateRandomMaterial();
            materials.Add(name, material);
            return material;
        }

        if (materialDto.Textures is null or { Count: 0 })
        {
            // TODO: might be water shaders and other special cases
            material = CreateRandomMaterial();
            materials.Add(name, material);
            return material;
        }

        var properties = materialDto.Shader is null
            ? Properties.Default
            : shaderProperties.GetValueOrDefault(materialDto.Shader.Replace('\\', '/'))
                ?? Properties.Default;

        var diffuseTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Diffuse"));
        var normalTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Normal"));

        material = CreateMaterial(diffuseTexture, normalTexture, properties);
        materials.Add(name, material);
        return material;
    }
}