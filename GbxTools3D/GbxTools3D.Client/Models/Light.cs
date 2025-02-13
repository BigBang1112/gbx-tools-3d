using GBX.NET;

namespace GbxTools3D.Client.Models;

public sealed class Light
{
    public required string Type { get; set; }
    public required bool IsActive { get; set; }
    public required Vec3 Color { get; set; }
    public required float FlareIntensity { get; set; }
    public required float Intensity { get; set; }
    public required float ShadowIntensity { get; set; }
    public float? ShadeMaxY { get; set; }
    public float? ShadeMinY { get; set; }
}