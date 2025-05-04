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

    [JSImport("createRotationXTrack", nameof(Animation))]
    public static partial JSObject CreateRotationXTrack(double[] times, double[] values);

    [JSImport("createRotationYTrack", nameof(Animation))]
    public static partial JSObject CreateRotationYTrack(double[] times, double[] values);

    [JSImport("createRelativePositionYTrack", nameof(Animation))]
    public static partial JSObject CreateRelativePositionYTrack(double[] times, double[] values, JSObject referenceObj);

    [JSImport("createClip", nameof(Animation))]
    public static partial JSObject CreateClip(string name, double duration, JSObject[] tracks);

    [JSImport("createMixer", nameof(Animation))]
    public static partial void CreateMixer(JSObject object3D);

    [JSImport("createAction", nameof(Animation))]
    public static partial JSObject CreateAction(JSObject clip, JSObject? object3D = null);

    [JSImport("playAction", nameof(Animation))]
    public static partial JSObject PlayAction(JSObject action);

    [JSImport("pauseAction", nameof(Animation))]
    public static partial JSObject PauseAction(JSObject action);

    [JSImport("resumeAction", nameof(Animation))]
    public static partial JSObject ResumeAction(JSObject action);

    [JSImport("playMixer", nameof(Animation))]
    public static partial void PlayMixer();

    [JSImport("pauseMixer", nameof(Animation))]
    public static partial void PauseMixer();

    [JSImport("setMixerTime", nameof(Animation))]
    public static partial void SetMixerTime(double time);
}