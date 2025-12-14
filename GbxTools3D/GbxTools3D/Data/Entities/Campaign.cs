using GBX.NET;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(GameVersion))]
[Index(nameof(Name))]
public sealed class Campaign
{
    public int Id { get; set; }

    public required GameVersion GameVersion { get; set; }

    [StringLength(24)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DisplayName { get; set; }

    [StringLength(byte.MaxValue)]
    public string? CollectionId { get; set; }

    public ICollection<MapGroup> MapGroups { get; set; } = [];
}
