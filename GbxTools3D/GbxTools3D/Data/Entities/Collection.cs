using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public class Collection
{
    public int Id { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public required string DisplayName { get; set; }

    public ICollection<BlockInfo> BlockInfos { get; set; } = [];
}
