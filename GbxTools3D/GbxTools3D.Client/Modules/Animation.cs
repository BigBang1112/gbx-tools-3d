using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
internal static partial class Animation
{
    [JSImport("createPositionTrack", nameof(Animation))]
    public static partial JSObject CreatePositionTrack(double[] times, double[] values);

    [JSImport("createQuaternionTrack", nameof(Animation))]
    public static partial JSObject CreateQuaternionTrack(double[] times, double[] values);

    [JSImport("createClip", nameof(Animation))]
    public static partial JSObject CreateClip(string name, double duration, JSObject[] tracks);

    [JSImport("createMixer", nameof(Animation))]
    public static partial void CreateMixer(JSObject object3D);

    [JSImport("createAction", nameof(Animation))]
    public static partial JSObject CreateAction(JSObject clip);

    [JSImport("playAction", nameof(Animation))]
    public static partial JSObject PlayAction(JSObject action);
}