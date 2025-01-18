using GBX.NET;

namespace GbxTools3D.Data.Entities;

public class BlockUnit
{
    public Int3 Offset { get; set; }
    public BlockClip[]? Clips { get; set; }
    public byte? AcceptPylons { get; set; }
    public byte? PlacePylons { get; set; }
}
