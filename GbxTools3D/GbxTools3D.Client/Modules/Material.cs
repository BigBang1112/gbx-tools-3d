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
        bool Invisible = false,
        bool Water = false,
        bool NoWrap = false,
        bool Substract = false)
    {
        public static readonly Properties Default = new();
    }

    private readonly record struct UniqueMaterial(string Name, GameVersion GameVersion, string? TerrainModifier = null)
    {
        public override string ToString() => $"{Name} | {GameVersion} | {TerrainModifier}";
    }

    private static readonly Dictionary<string, Properties> shaderProperties = new()
    {
        ["Techno/Media/Material/PDiff PDiff PA PX2"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA PX2 Grass2"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA TOcc PX2 Grass"] = new(WorldUV: true),
        ["Techno2/Media/Material/PDiff PDiff PA TOcc PX2 Grass NoLightV"] = new(WorldUV: true),
        ["Techno2/Media/Material/TDiff_Spec_Nrm TOcc CSpecSoft"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans"] = new(Transparent: true, DoubleSided: true),
        ["Techno/Media/Material/TDiff PX2 Trans 2Sided"] = new Properties(DoubleSided: true, Transparent: true),
        ["Techno/Media/Material/TDiffG PX2 CSpec FCOut Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiffG PX2 CSpecL Trans"] = new(Transparent: true),
        ["Techno/Media/Material/TDiff PX2 Trans NormY PC3only"] = new(DoubleSided: true, Transparent: true),
        ["Techno/Media/Material/PDiff Fresnel PX2"] = new(WorldUV: true),
        ["Techno/Media/Material/Sky"] = new Properties(Basic: true),
        //["Techno/Media/Material/PDiff TDiffA PX2"] = new(WorldUV: true),
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
        ["Techno3/Media/Material/Tech3 Block TDiffA_Spec_Norm"] = new(Transparent: true),
        ["Techno3/Media/Material/Tech3 Block PDiff_Spec_Norm"] = new(WorldUV: true),
        ["Techno3/Media/Material/Tech3 Block PDiff_Spec_Norm GrassX2"] = new(WorldUV: true),
        //["Techno3/Media/Material/Tech3 Block PTDiff_Spec_Norm PGrassX2"] = new(WorldUV: true)
        ["Techno/Media/Material/Sea"] = new(Water: true),
        ["Techno/Media/Material/SeaMultiY"] = new(Water: true),
        ["Techno3/Media/Material/Tech3 Sea"] = new(Water: true),
        ["Techno3/Media/Material/Tech3_Block_TDiffABlend_SpecNorm_CubeOut"] = new(Transparent: true),
        ["Island/Media/Material/IslandBeachFoam"] = new(Add: true, NoWrap: true),
        [":Glass"] = new(Transparent: true, Opacity: 0.89),
        [":FakeShad"] = new(Invisible: true),
        ["Techno3/Media/Material/Sky/Tech3 Sky"] = new Properties(Invisible: true),
        ["Techno3/Media/Material/Tech3 Warp PyaPxzDiff"] = new(WorldUV: true),
        ["Techno3/Media/Material/Tech3 Warp_PyaDiff_To_PDiffPGrassX2"] = new(WorldUV: true),
        ["Island/Media/Material/IslandSky"] = new(Basic: true/*, Invisible: true*/),
        ["Sky/Media/Material/SkyDay"] = new(Basic: true/*, Invisible: true*/),
        ["Island/Media/Material/IslandWindowsMip"] = new(Transparent: true, Opacity: 0.7),
    };

    private static readonly Dictionary<(string, GameVersion), JSObject> textures = [];
    private static readonly Dictionary<UniqueMaterial, JSObject> materials = [];

    [JSImport("getWireframeMaterial", nameof(Material))]
    public static partial JSObject GetWireframeMaterial();

    [JSImport("createRandomMaterial", nameof(Material))]
    private static partial JSObject CreateRandomMaterial();

    [JSImport("createInvisibleMaterial", nameof(Material))]
    private static partial JSObject CreateInvisibleMaterial();

    [JSImport("createGlassMaterial", nameof(Material))]
    private static partial JSObject CreateGlassMaterial();

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
        bool invisible,
        bool water,
        bool substract);

    [JSImport("createTexture", nameof(Material))]
    private static partial JSObject CreateTexture(string path, string urlPath, bool noWrap);
    
    private static JSObject? GetOrCreateTexture(string? path, GameVersion gameVersion, bool noWrap)
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
        texture = CreateTexture(path, $"api/texture/{hash}", noWrap);
        textures.Add((path, gameVersion), texture);
        
        return texture;
    }
    
    public static JSObject GetOrCreateMaterial(string name, GameVersion gameVersion, Dictionary<string, MaterialDto>? availableMaterials, string? terrainModifier)
    {
        var uniqueMaterial = new UniqueMaterial(name, gameVersion, terrainModifier);

        if (materials.TryGetValue(uniqueMaterial, out var material))
        {
            return material;
        }

        if (availableMaterials is null || !availableMaterials.TryGetValue(name, out var materialDto))
        {
            material = CreateRandomMaterial();
            materials.Add(uniqueMaterial, material);
            return material;
        }

        if (terrainModifier is not null && materialDto.Modifiers?.TryGetValue(terrainModifier, out var modifierDto) == true)
        {
            materialDto = modifierDto;
        }

        if (materialDto.IsShader && materialDto.Textures is null or { Count: 0 }) // BayCollision for example
        {
            if (name == ":Glass")
            {
                material = CreateGlassMaterial();
            }
            else
            {
                material = CreateInvisibleMaterial();
            }
            materials.Add(uniqueMaterial, material);
            return material;
        }

        if (materialDto.Textures is null or { Count: 0 })
        {
            // TODO: might be water shaders and other special cases
            material = CreateRandomMaterial();
            materials.Add(uniqueMaterial, material);
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

        var ignoreNormal = gameVersion >= GameVersion.TMT;

        var diffuseTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Diffuse")
            ?? materialDto.Textures.GetValueOrDefault("Blend1")
            ?? materialDto.Textures.GetValueOrDefault("Panorama")
            ?? materialDto.Textures.GetValueOrDefault("Advert")
            ?? materialDto.Textures.GetValueOrDefault("Glow")
            ?? materialDto.Textures.GetValueOrDefault("Soil")
            ?? materialDto.Textures.GetValueOrDefault("Grass")
            ?? materialDto.Textures.GetValueOrDefault("Foam 1")
            ?? materialDto.Textures.GetValueOrDefault("GDiffuse")
            ?? materialDto.Textures.GetValueOrDefault("PyDiffuse")
            ?? materialDto.Textures.GetValueOrDefault("PxzDiffuse"), gameVersion, properties.NoWrap);
        var normalTexture = ignoreNormal ? null : GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Normal")
            // ?? materialDto.Textures.GetValueOrDefault("PxzNormal")  these kinds of normals are not quite working as expected
            , gameVersion, properties.NoWrap);
        var specularTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Specular")
            ?? materialDto.Textures.GetValueOrDefault("PxzSpecular"), gameVersion, properties.NoWrap);
        var blend2Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend2"), gameVersion, properties.NoWrap);
        var blendIntensityTexture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("BlendI"), gameVersion, properties.NoWrap);
        var blend3Texture = GetOrCreateTexture(materialDto.Textures.GetValueOrDefault("Blend3")
            ?? materialDto.Textures.GetValueOrDefault("Borders")
            ?? materialDto.Textures.GetValueOrDefault("SoilFix")
            ?? materialDto.Textures.GetValueOrDefault("DiffuseBlendA"), gameVersion, properties.NoWrap);
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
            properties.Invisible,
            properties.Water,
            properties.Substract);
        materials.Add(uniqueMaterial, material);
        return material;
    }
}