using System.Runtime.InteropServices.JavaScript;

namespace GbxTools3D.Client.Modules;

public partial class Slide
{
    [JSImport("doSlide", nameof(Slide))]
    public static partial void Do(JSObject wheelObj, JSObject scene);

    [JSImport("stopSlide", nameof(Slide))]
    public static partial void Stop(JSObject wheelObj);
}
