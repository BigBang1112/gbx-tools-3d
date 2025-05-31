using GBX.NET;
using System.Runtime.InteropServices.JavaScript;

namespace GbxTools3D.Client.Modules;

internal sealed partial class Camera(double fov = 80)
{
    public JSObject Object { get; } = Create(fov);

    [JSImport("create", nameof(Camera))]
    private static partial JSObject Create(double fov);
    
    [JSImport("createMapControls", nameof(Camera))]
    private static partial void CreateMapControls(JSObject camera, JSObject renderer, double targetX, double targetY, double targetZ);

    [JSImport("createOrbitControls", nameof(Camera))]
    private static partial void CreateOrbitControls(JSObject camera, JSObject renderer, double targetX, double targetY, double targetZ);

    [JSImport("setPosition", nameof(Camera))]
    private static partial void SetPosition(JSObject camera, double x, double y, double z);

    [JSImport("follow", nameof(Camera))]
    public static partial void Follow(JSObject target, double far, double up, double lookAtFactor);

    [JSImport("unfollow", nameof(Camera))]
    public static partial void Unfollow();

    public Vec3 Position
    {
        set => SetPosition(Object, value.X, value.Y, value.Z);
    }

    public void CreateMapControls(JSObject renderer, Vec3 target)
    {
        CreateMapControls(Object, renderer, target.X, target.Y, target.Z);
    }

    public void CreateOrbitControls(JSObject renderer, Vec3 target)
    {
        CreateOrbitControls(Object, renderer, target.X, target.Y, target.Z);
    }
}
