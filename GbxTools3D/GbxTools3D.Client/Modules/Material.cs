using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using GBX.NET;
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
        bool Basic = false,
        bool SpecularAlpha = false,
        double Opacity = 0,
        bool Add = false,
        bool NightOnly = false,
        bool Invisible = false)
    {
        public static readonly Properties Default = new();
    }

    private static readonly Dictionary<string, Properties> shaderProperties = new()
    {
        ["Techno2/Media/Material/PDiff PDiff PA PX2 Grass2"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA TOcc PX2 Grass"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA TOcc PX2 Grass NoLightV"] = new(WorldUV: true),
        ["Techno2/Media/Material/TDiff_Spec_Nrm TOcc CSpecSoft"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans 2Sided"] = new Properties(DoubleSided: true, Transparent: true),
        ["Techno/Media/Material/TDiffG PX2 CSpec FCOut Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiffG PX2 CSpecL Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans NormY PC3only"] = new(DoubleSided: true, Transparent: true),
        ["Techno/Media/Material/PDiff Fresnel PX2"] = new(WorldUV: true),
        ["Techno/Media/Material/Sky"] = new Properties(Basic: true),
        ["Techno/Media/Material/PDiff PDiff PA PX2"] = new(WorldUV: true),
        ["Techno/Media/Material/TDiffG PX2 CSpecL_Pixel"] = new Properties(SpecularAlpha: true),
        ["Vehicles/Media/Material/SportCarGlass"] = new(Transparent: true, Opacity: 0.89),
        ["Techno/Media/Material/TAdd"] = new(Transparent: true, Add: true),
        ["Techno/Media/Material/TAdd ZBias"] = new(Transparent: true, Add: true),
        ["Techno/Media/Material/TAdd Night"] = new(Transparent: true, Add: true, NightOnly: true),
        ["Techno/Media/Material/TAdd Night ZBias"] = new(Transparent: true, Add: true, NightOnly: true),
        ["Island/Media/Material/ModelLightVolume"] = new(Transparent: true, Add: true, NightOnly: true),
        ["Alpine/Media/Material/AlpineSignsSelfIllum"] = new(Transparent: true, Add: true),
        ["Techno2/Media/Material/TSelfI Add"] = new(Transparent: true, Add: true),
        ["Techno2/Media/Material/VDep Fence"] = new(Invisible: true),
        ["Techno/Media/Material/ShadowSkirt"] = new(Invisible: true),
        ["Techno2/Media/Material/TDiff_Spec_Nrm TOcc CSpecSoft NoLightV"] = new(Transparent: true),
        ["Techno/Media/Material/PDisp PDiff PX2"] = new(WorldUV: true),
        //["Techno/Media/Material/PDiff PDiff PA TDiffA PX2"] = new(WorldUV: true), WorldUVDiffuse
        ["Island/Media/Material/ModelAlpha1SidedLight"] = new(Transparent: true),
        ["Island/Media/Material/ModelAlpha2SidedNoLight"] = new(Transparent: true, DoubleSided: true),
        ["Techno2/Media/Material/SoilGen21"] = new(WorldUV: true),
        ["Techno2/Media/Material/TDiff_Spec_Nrm TOcc CSpecSoft Trans"] = new(Transparent: true),
    };

    private static readonly Dictionary<(string, GameVersion), JSObject> textures = [];
    private static readonly Dictionary<(string, GameVersion), JSObject> materials = [];

    [JSImport("getWireframeMaterial", nameof(Material))]
    public static partial JSObject GetWireframeMaterial();

    [JSImport("createRandomMaterial", nameof(Material))]
    private static partial JSObject CreateRandomMaterial();

    [JSImport("createInvisibleMaterial", nameof(Material))]
    private static partial JSObject CreateInvisibleMaterial();

    [JSImport("createMaterial", nameof(Material))]
    private static partial JSObject CreateMaterial(
        string materialName,
        string? shaderName,
        JSObject? diffuseTexture, 
        JSObject? normalTexture,
        JSObject? specularTexture,
        JSObject? blend2Texture,
        JSObject? blendIntensityTexture,
        JSObject? blend3Texture,
        JSObject? aoTexture,
        bool doubleSided,
        bool worldUV,
        bool transparent,
        bool basic,
        bool specularAlpha,
        double opacity,
        bool add,
        bool nightOnly,
        bool invisible);

    [JSImport("createTexture", nameof(Material))]
    private static partial JSObject CreateTexture(string path, string urlPath);
    
    private static JSObject? GetOrCreateTexture(string? path, GameVersion gameVersion)
    {
        if (path is null)
        {
            return null;
        }

        if (textures.TryGetValue((path, gameVersion), out var texture))
        {
            return texture;
        }
        
        var hash = $"GbxTools3D|Texture|{gameVersion}|{path}|PeopleOnTheBusLikeDMCA".Hash();
        texture = CreateTexture(path, $"api/texture/{hash}");
        textures.Add((path, gameVersion), texture);
        
        return texture;
    }
    
    public static JSObject GetOrCreateMaterial(string name, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials)
    {
        if (materials.TryGetValue((name, gameVersion), out var material))
        {
            return material;
        }

        if (availableMaterials is null || !availableMaterials.TryGetValue(name, out var materialDto))
        {
            material = CreateRandomMaterial();
            materials.Add((name, gameVersion), material);
            return material;
        }

        if (materialDto.IsShader && materialDto.Textures is null or { Count: 0 }) // BayCollision for example
        {
            material = CreateInvisibleMaterial();
            materials.Add((name, gameVersion), material);
            return material;
        }

        if (materialDto.Textures is null or { Count: 0 })
        {
            // TODO: might be water shaders and other special cases
            material = CreateRandomMaterial();
            materials.Add((name, gameVersion), material);
            return material;
        }

        if (materialDto.Shader is not null && shaderProperties.TryGetValue(materialDto.Shader.Replace('\\', '/'), out Properties? properties))
        {
            // the shader is available in the shaderProperties
        }
        else if (materialDto.IsShader && shaderProperties.TryGetValue(name.Replace('\\', '/'), out properties))
        {
            // the material is an actual shader with textures, there could be some shader properties
        }
        else
        {
            properties = Properties.Default;
        }

        var diffuseTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Diffuse"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend1"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Panorama"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Advert"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Glow"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Soil"), gameVersion)
            ?? GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Grass"), gameVersion);
        var normalTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Normal"), gameVersion);
        var specularTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Specular"), gameVersion);
        var blend2Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend2"), gameVersion);
        var blendIntensityTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("BlendI"), gameVersion);
        var blend3Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend3") ?? materialDto.Textures.GetValueOrDefault("Borders"), gameVersion);
        var aoTexture = default(JSObject);//GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Occlusion"), gameVersion);

        material = CreateMaterial(
            name,
            materialDto.Shader,
            diffuseTexture, 
            normalTexture,
            specularTexture,
            blend2Texture,
            blendIntensityTexture,
            blend3Texture,
            aoTexture,
            properties.DoubleSided, 
            properties.WorldUV, 
            properties.Transparent,
            properties.Basic,
            properties.SpecularAlpha,
            properties.Opacity,
            properties.Add,
            properties.NightOnly,
            properties.Invisible);
        materials.Add((name, gameVersion), material);
        return material;
    }
}