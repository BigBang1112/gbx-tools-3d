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
    
    public float XX { get; set; }
    public float XY { get; set; }
    public float XZ { get; set; }
    public float YX { get; set; }
    public float YY { get; set; }
    public float YZ { get; set; }
    public float ZX { get; set; }
    public float ZY { get; set; }
    public float ZZ { get; set; }
    public float TX { get; set; }
    public float TY { get; set; }
    public float TZ { get; set; }
}