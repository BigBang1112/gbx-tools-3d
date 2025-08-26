using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class MapGroup
{
    public int Id { get; set; }

    [StringLength(32)]
    public required string Name { get; set; }

    public required Campaign Campaign { get; set; }

    public int Order { get; set; }

    public ICollection<Map> Maps { get; set; } = [];
}
