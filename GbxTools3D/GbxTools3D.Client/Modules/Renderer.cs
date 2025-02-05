using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal static partial class Renderer
{
    [JSImport("create", nameof(Renderer))]
    public static partial JSObject Create();

    [JSImport("setScene", nameof(Renderer))]
    private static partial void SetScene(JSObject scene);

    [JSImport("setControls", nameof(Renderer))]
    public static partial void SetControls(JSObject controls);

    [JSImport("setCamera", nameof(Renderer))]
    public static partial void SetCamera(JSObject camera);

    public static Scene Scene
    {
        set => SetScene((JSObject)value);
    }
}
