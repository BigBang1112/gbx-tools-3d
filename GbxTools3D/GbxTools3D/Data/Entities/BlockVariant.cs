using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class BlockVariant
{
    public int Id { get; set; }

    public int BlockInfoId { get; set; }
    public required BlockInfo BlockInfo { get; set; }

    public bool Ground { get; set; }
    public byte Variant { get; set; }
    public byte SubVariant { get; set; }

    public int MeshId { get; set; }
    public required Mesh Mesh { get; set; }
    
    [StringLength(byte.MaxValue)]
    public string? Path { get; set; }

    public ICollection<ObjectLink> ObjectLinks { get; set; } = [];
}