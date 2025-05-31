using GBX.NET;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class ObjectLink
{
    public int Id { get; set; }
    public required int Index { get; set; }
    
    public int VariantId { get; set; }
    public required BlockVariant Variant { get; set; }

    public int MeshId { get; set; }
    public required Mesh Mesh { get; set; }
    
    [StringLength(byte.MaxValue)]
    public string? Path { get; set; }

    public Iso4 Loc { get; set; } = Iso4.Identity;

    public Sound? Sound { get; set; }
    public Iso4 SoundLoc { get; set; } = Iso4.Identity;
}