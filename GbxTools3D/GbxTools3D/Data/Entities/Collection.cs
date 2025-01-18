using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public class Collection
{
    [StringLength(byte.MaxValue)]
    public required string Id { get; set; }

    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    public ICollection<BlockInfo> BlockInfos { get; set; } = [];
}
