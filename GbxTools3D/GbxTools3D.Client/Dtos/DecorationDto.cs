using System.Collections.Immutable;

namespace GbxTools3D.Client.Dtos;

public sealed class DecorationDto
{
    public required string Name { get; set; }
    public required ImmutableDictionary<string, string> Musics { get; set; }
    public required ImmutableDictionary<string, string> Sounds { get; set; }
    public string? Remap { get; set; }
    public string? TerrainModifierCovered { get; set; } // Fabric
}