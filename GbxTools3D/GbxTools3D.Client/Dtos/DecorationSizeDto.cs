using GBX.NET;
using GbxTools3D.Client.Models;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Dtos;

public sealed class DecorationSizeDto
{
    public required Int3 Size { get; set; }
    public int BaseHeight { get; set; }
    public required ImmutableArray<SceneObject> Scene { get; set; }
    public required ImmutableList<DecorationDto> Decorations { get; set; } = [];
}