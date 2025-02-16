using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Extensions;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal sealed partial class Material
{
    private static readonly Dictionary<string, JSObject> textures = [];
    private static readonly Dictionary<string, JSObject> materials = [];
    
    [JSImport("get", nameof(Material))]
    private static partial JSObject Get();
    
    [JSImport("getWithTexture", nameof(Material))]
    private static partial JSObject GetWithTexture(JSObject texture);
    
    [JSImport("loadTexture", nameof(Material))]
    private static partial JSObject LoadTexture(string path);
    
    private static JSObject GetTexture(string path)
    {
        if (textures.TryGetValue(path, out var texture))
        {
            return texture;
        }
        
        var hash = $"GbxTools3D|Texture|TMF|{path}|PeopleOnTheBusLikeDMCA".Hash();
        texture = LoadTexture($"api/texture/{hash}");
        textures.Add(path, texture);
        
        return texture;
    }
    
    public static JSObject Get(string name, Dictionary<string, MaterialDto>? availableMaterials)
    {
        if (materials.TryGetValue(name, out var material))
        {
            return material;
        }
        
        if (availableMaterials?.TryGetValue(name, out var materialDto) == true)
        {
            if (materialDto.Textures?.Count > 0)
            {
                if (materialDto.Textures.TryGetValue("Diffuse", out var texturePath))
                {
                    material = GetWithTexture(GetTexture(texturePath));
                }
                else
                {
                    material = GetWithTexture(GetTexture(materialDto.Textures.Values.First()));
                }
            }
            else
            {
                material = Get();
            }
        }
        else
        {
            material = Get();
        }

        materials.Add(name, material);

        return material;
    }
}