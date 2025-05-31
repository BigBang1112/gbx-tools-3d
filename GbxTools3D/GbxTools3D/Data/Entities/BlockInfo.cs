using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using GBX.NET;
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

    public ImmutableArray<BlockUnit> AirUnits { get; set; } = [];
    public ImmutableArray<BlockUnit> GroundUnits { get; set; } = [];
    
    public bool HasAirHelper { get; set; }
    public bool HasGroundHelper { get; set; }
    public bool HasConstructionModeHelper { get; set; }
    
    public byte? Height { get; set; }

    public int? IconId { get; set; }
    public Icon? Icon { get; set; }

    public bool IsRoad { get; set; }

    [StringLength(64)]
    public string? PylonName { get; set; }

    public Iso4 SpawnLocAir { get; set; } = Iso4.Identity;
    public Iso4 SpawnLocGround { get; set; } = Iso4.Identity;

    public ICollection<BlockVariant> Variants { get; set; } = [];

    public override string ToString()
    {
        return Name;
    }
}
