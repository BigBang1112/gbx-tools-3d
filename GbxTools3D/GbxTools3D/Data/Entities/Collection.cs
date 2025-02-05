using System.ComponentModel.DataAnnotations;
using GBX.NET;

namespace GbxTools3D.Data.Entities;

public class Collection
{
    public int Id { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DisplayName { get; set; }

    public required GameVersion GameVersion { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BlockInfo> BlockInfos { get; set; } = [];
}
