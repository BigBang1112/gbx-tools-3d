using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(SizeX), nameof(SizeY), nameof(SizeZ))]
public sealed class DecorationSize
{
    public int Id { get; set; }
    public int SizeX { get; set; }
    public int SizeY { get; set; }
    public int SizeZ { get; set; }
    public int BaseHeight { get; set; }
}