using GBX.NET;

namespace GbxTools3D.Client.Models;

public sealed class SceneObject
{
    public string? Solid { get; set; }
    public Light? Light { get; set; }
    public Iso4 Location { get; set; }
}