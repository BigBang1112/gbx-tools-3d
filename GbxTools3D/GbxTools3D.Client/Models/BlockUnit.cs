using GBX.NET;

namespace GbxTools3D.Client.Models;

public sealed record BlockUnit
{
    public Int3 Offset { get; set; }
    public BlockClip[]? Clips { get; set; }
    public byte? AcceptPylons { get; set; }
    public byte? PlacePylons { get; set; }
}
