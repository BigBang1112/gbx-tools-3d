using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Name))]
public sealed class TerrainModifier
{
    public int Id { get; set; }

    public int CollectionId { get; set; }
    public required Collection Collection { get; set; }

    [StringLength(64)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public required string RemapFolder { get; set; }

    public ICollection<Material> Materials { get; set; } = [];

    public override string ToString()
    {
        return Name;
    }
}
