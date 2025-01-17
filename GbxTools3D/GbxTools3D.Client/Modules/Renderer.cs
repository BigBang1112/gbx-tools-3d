using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal static partial class Renderer
{
    [JSImport("create", nameof(Renderer))]
    public static partial JSObject Create();

    [JSImport("setScene", nameof(Renderer))]
    public static partial void SetScene(JSObject scene);

    public static Scene Scene
    {
        set => SetScene((JSObject)value);
    }
}
