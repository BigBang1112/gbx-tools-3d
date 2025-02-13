using System.Runtime.InteropServices.JavaScript;

namespace GbxTools3D.Client.Modules;

internal sealed partial class Camera
{
    [JSImport("create", nameof(Camera))]
    public static partial JSObject Create();
    
    [JSImport("createMapControls", nameof(Camera))]
    public static partial void CreateMapControls(JSObject camera, JSObject renderer, double targetX, double targetY, double targetZ);
    
    [JSImport("setPosition", nameof(Camera))]
    public static partial void SetPosition(JSObject camera, double x, double y, double z);
    
    [JSImport("lookAt", nameof(Camera))]
    public static partial void LookAt(JSObject camera, double x, double y, double z);
}
