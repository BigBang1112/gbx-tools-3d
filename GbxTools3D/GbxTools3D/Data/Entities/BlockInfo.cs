using System.ComponentModel.DataAnnotations;
using GbxTools3D.Client.Models;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Name))]
public class BlockInfo
{
    public int Id { get; set; }

    public int CollectionId { get; set; }
    public required Collection Collection { get; set; }

    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    public BlockUnit[] AirUnits { get; set; } = [];
    public BlockUnit[] GroundUnits { get; set; } = [];
    
    public byte? Height { get; set; }

    public ICollection<BlockVariant> Variants { get; set; } = [];

    public override string ToString()
    {
        return Name;
    }
}
