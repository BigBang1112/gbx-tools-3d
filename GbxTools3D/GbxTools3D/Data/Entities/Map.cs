using GBX.NET;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbxTools3D.Data.Entities;

[Index(nameof(GameVersion))]
[Index(nameof(MapUid))]
public sealed class Map
{
    public int Id { get; set; }

    public required GameVersion GameVersion { get; set; }

    [StringLength(64)]
    public required string MapUid { get; set; }

    [StringLength(byte.MaxValue)]
    public string? Path { get; set; }

    public MapGroup? Group { get; set; }

    [Column(TypeName = "mediumblob")]
    public required byte[] Data { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
