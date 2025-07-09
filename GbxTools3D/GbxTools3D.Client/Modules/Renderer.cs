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

    [JSImport("enableRaycaster", nameof(Renderer))]
    public static partial void EnableRaycaster();

    [JSImport("disableRaycaster", nameof(Renderer))]
    public static partial void DisableRaycaster();

    [JSImport("attachTransformControls", nameof(Renderer))]
    public static partial void AttachTransformControls(JSObject obj);

    [JSImport("detachTransformControls", nameof(Renderer))]
    public static partial void DetachTransformControls();

    [JSImport("showTransformControls", nameof(Renderer))]
    public static partial void ShowTransformControls();

    [JSImport("hideTransformControls", nameof(Renderer))]
    public static partial void HideTransformControls();

    [JSImport("setTransformControlsAxis", nameof(Renderer))]
    public static partial void SetTransformControlsAxis(bool x, bool y, bool z);

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
