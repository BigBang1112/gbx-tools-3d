using System.ComponentModel.DataAnnotations;
using GbxTools3D.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Name))]
public sealed class BlockInfo
{
    public int Id { get; set; }

    public int CollectionId { get; set; }
    public required Collection Collection { get; set; }

    [StringLength(64)]
    public required string Name { get; set; }

    public BlockUnit[] AirUnits { get; set; } = [];
    public BlockUnit[] GroundUnits { get; set; } = [];
    
    public bool HasAirHelper { get; set; }
    public bool HasGroundHelper { get; set; }
    public bool HasConstructionModeHelper { get; set; }
    
    public byte? Height { get; set; }

    public int? IconId { get; set; }
    public Icon? Icon { get; set; }

    public ICollection<BlockVariant> Variants { get; set; } = [];

    public override string ToString()
    {
        return Name;
    }
}
