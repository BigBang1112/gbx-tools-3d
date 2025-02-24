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

    [JSImport("setCamera", nameof(Renderer))]
    private static partial void SetCamera(JSObject camera);

    [JSImport("dispose", nameof(Renderer))]
    public static partial void Dispose();

    public static Scene Scene
    {
        set => SetScene((JSObject)value);
    }

    public static Camera Camera
    {
        set => SetCamera(value.Object);
    }
}
