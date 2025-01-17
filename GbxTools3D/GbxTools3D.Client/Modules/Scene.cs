using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal partial class Scene
{
    private readonly JSObject scene = Create();

    [JSImport("create", nameof(Scene))]
    private static partial JSObject Create();

    [JSImport("add", nameof(Scene))]
    private static partial void Add(JSObject scene, JSObject obj);

    [JSImport("test", nameof(Scene))]
    private static partial void Test(JSObject scene);

    public void Add(JSObject obj)
    {
        Add(scene, obj);
    }

    public void Test()
    {
        Test(scene);
    }

    public static explicit operator JSObject(Scene scene) => scene.scene;
}