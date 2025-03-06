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
        bool Transparent = false,
        bool Basic = false)
    {
        public static readonly Properties Default = new();
    }

    private static readonly Dictionary<string, Properties> shaderProperties = new()
    {
        ["Techno2/Media/Material/PDiff PDiff PA PX2 Grass2"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA TOcc PX2 Grass"] = new(WorldUV: true),
        ["Techno2/Media/Material/TDiff_Spec_Nrm TOcc CSpecSoft"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans 2Sided"] = new Properties(DoubleSided: true, Transparent: true),
        //["Techno/Media/Material/PDiff Fresnel PX2"] = new(WorldUV: true),
        ["Techno/Media/Material/Sky"] = new Properties(Basic: true)
    };

    private static readonly Dictionary<string, JSObject> textures = [];
    private static readonly Dictionary<string, JSObject> materials = [];
    
    [JSImport("createRandomMaterial", nameof(Material))]
    private static partial JSObject CreateRandomMaterial();

    [JSImport("createInvisibleMaterial", nameof(Material))]
    private static partial JSObject CreateInvisibleMaterial();

    [JSImport("createMaterial", nameof(Material))]
    private static partial JSObject CreateMaterial(
        JSObject? diffuseTexture, 
        JSObject? normalTexture,
        JSObject? specularTexture,
        JSObject? blend2Texture,
        JSObject? blendIntensityTexture,
        JSObject? blend3Texture,
        bool doubleSided,
        bool worldUV,
        bool transparent,
        bool basic);

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

        if (materialDto.IsShader) // BayCollision for example
        {
            material = CreateInvisibleMaterial();
            materials.Add(name, material);
            return material;
        }


        if (materialDto.Shader is not null && shaderProperties.TryGetValue(materialDto.Shader.Replace('\\', '/'), out Properties? properties))
        {

        }
        else
        {
            properties = Properties.Default;
        }

        var diffuseTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Diffuse"))
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend1"))
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Panorama"));
        var normalTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Normal"));
        var specularTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Specular"));
        var blend2Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend2"));
        var blendIntensityTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("BlendI"));
        var blend3Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend3"));

        material = CreateMaterial(
            diffuseTexture, 
            normalTexture,
            specularTexture,
            blend2Texture,
            blendIntensityTexture,
            blend3Texture,
            properties.DoubleSided, 
            properties.WorldUV, 
            properties.Transparent,
            properties.Basic);
        materials.Add(name, material);
        return material;
    }
}