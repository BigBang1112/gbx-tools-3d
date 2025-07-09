using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal partial class Scene
{
    public JSObject Object { get; }

    [JSImport("create", nameof(Scene))]
    private static partial JSObject Create(bool isCatalog);

    [JSImport("add", nameof(Scene))]
    private static partial void Add(JSObject scene, JSObject obj);

    [JSImport("remove", nameof(Scene))]
    private static partial void Remove(JSObject scene, JSObject obj);

    [JSImport("clear", nameof(Scene))]
    private static partial void Clear(JSObject scene);

    [JSImport("test", nameof(Scene))]
    private static partial void Test(JSObject scene);

    [JSImport("toggleGrid", nameof(Scene))]
    private static partial void ToggleGrid(JSObject scene, bool visible);

    [JSImport("getObjectById", nameof(Scene))]
    private static partial JSObject? GetObjectById(JSObject scene, int objectId);

    [JSImport("getObjectByName", nameof(Scene))]
    private static partial JSObject? GetObjectByName(JSObject scene, string name);

    public Scene(bool isCatalog)
    {
        Object = Create(isCatalog);
    }

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

    public void ShowGrid()
    {
        ToggleGrid(Object, visible: true);
    }

    public void HideGrid()
    {
        ToggleGrid(Object, visible: false);
    }

    public JSObject? GetObjectById(int objectId)
    {
        return GetObjectById(Object, objectId);
    }

    public JSObject? GetObjectByName(string name)
    {
        return GetObjectByName(Object, name);
    }

    public static explicit operator JSObject(Scene scene) => scene.Object;
}