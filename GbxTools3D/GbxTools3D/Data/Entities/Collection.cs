using System.ComponentModel.DataAnnotations;
using GBX.NET;

namespace GbxTools3D.Data.Entities;

public sealed class Collection
{
    public int Id { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DisplayName { get; set; }

    public required GameVersion GameVersion { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int SquareHeight { get; set; }
    public int SquareSize { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string VehicleId { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string VehicleCollection { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string VehicleAuthor { get; set; }
    
    [StringLength(byte.MaxValue)]
    public string? DefaultZoneBlock { get; set; }
    
    public int SortIndex { get; set; }

    public ICollection<BlockInfo> BlockInfos { get; set; } = [];
}
