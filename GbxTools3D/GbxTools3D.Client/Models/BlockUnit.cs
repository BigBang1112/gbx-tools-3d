using GBX.NET;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Models;

public sealed record BlockUnit
{
    public Byte3 Offset { get; set; }
    public ImmutableArray<BlockClip>? Clips { get; set; }
    public byte? AcceptPylons { get; set; }
    public byte? PlacePylons { get; set; }
    public string? TerrainModifier { get; set; }
}
