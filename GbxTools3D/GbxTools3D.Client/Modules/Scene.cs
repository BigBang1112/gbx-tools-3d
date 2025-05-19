using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal partial class Scene
{
    public JSObject Object { get; } = Create();

    [JSImport("create", nameof(Scene))]
    private static partial JSObject Create();

    [JSImport("add", nameof(Scene))]
    private static partial void Add(JSObject scene, JSObject obj);

    [JSImport("remove", nameof(Scene))]
    private static partial void Remove(JSObject scene, JSObject obj);

    [JSImport("clear", nameof(Scene))]
    private static partial void Clear(JSObject scene);

    [JSImport("test", nameof(Scene))]
    private static partial void Test(JSObject scene);

    public void Add(JSObject obj)
    {
        Add(Object, obj);
    }

    public void Add(Solid solid)
    {
        Add(Object, solid.Object);
    }

    public void Remove(JSObject obj)
    {
        Remove(Object, obj);
    }

    public void Remove(Solid solid)
    {
        Remove(Object, solid.Object);
    }

    public void Clear()
    {
        Clear(Object);
    }

    public void Test()
    {
        Test(Object);
    }

    public static explicit operator JSObject(Scene scene) => scene.Object;
}