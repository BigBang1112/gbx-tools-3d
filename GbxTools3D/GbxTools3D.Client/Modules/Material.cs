using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal sealed partial class Material
{
    private static readonly Dictionary<string, JSObject> materials = [];
    
    [JSImport("get", nameof(Material))]
    private static partial JSObject Get();
    
    public static JSObject Get(string name)
    {
        if (materials.TryGetValue(name, out var material))
        {
            return material;
        }

        material = Get();
        materials.Add(name, material);

        return material;
    }
}