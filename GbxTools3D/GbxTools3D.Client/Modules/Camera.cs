using GBX.NET;
using System.Runtime.InteropServices.JavaScript;

namespace GbxTools3D.Client.Modules;

internal sealed partial class Camera
{
    public JSObject Object { get; } = Create();

    [JSImport("create", nameof(Camera))]
    private static partial JSObject Create();
    
    [JSImport("createMapControls", nameof(Camera))]
    private static partial void CreateMapControls(JSObject camera, JSObject renderer, double targetX, double targetY, double targetZ);
    
    [JSImport("setPosition", nameof(Camera))]
    private static partial void SetPosition(JSObject camera, double x, double y, double z);
    
    [JSImport("lookAt", nameof(Camera))]
    public static partial void LookAt(JSObject camera, double x, double y, double z);

    public Vec3 Position
    {
        set => SetPosition(Object, value.X, value.Y, value.Z);
    }

    public void CreateMapControls(JSObject renderer, Vec3 target)
    {
        CreateMapControls(Object, renderer, target.X, target.Y, target.Z);
    }
}
