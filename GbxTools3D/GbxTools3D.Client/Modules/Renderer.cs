using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Modules;

[SupportedOSPlatform("browser")]
public static partial class Renderer
{
    [JSImport("create", nameof(Renderer))]
    internal static partial JSObject Create();
}
