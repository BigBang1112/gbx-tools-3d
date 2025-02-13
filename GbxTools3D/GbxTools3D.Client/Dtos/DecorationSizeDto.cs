using GBX.NET;
using GbxTools3D.Client.Models;

namespace GbxTools3D.Client.Dtos;

public sealed class DecorationSizeDto
{
    public required Int3 Size { get; set; }
    public int BaseHeight { get; set; }
    public required SceneObject[] Scene { get; set; }
    public required List<DecorationDto> Decorations { get; set; } = [];
}