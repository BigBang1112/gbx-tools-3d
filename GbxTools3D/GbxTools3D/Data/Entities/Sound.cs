using GBX.NET;
using GbxTools3D.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Hash), IsUnique = true)]
[Index(nameof(Path), nameof(GameVersion))]
public sealed class Sound
{
    public int Id { get; set; }

    public required GameVersion GameVersion { get; set; }

    [MinLength(52), MaxLength(52)]
    public required string Hash { get; set; }

    [MaxLength(16_777_215)]
    public required byte[] Data { get; set; }

    [StringLength(byte.MaxValue)]
    public required string Path { get; set; }

    [Required]
    public SoundType Type { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(255)]
    public string? AudioPath { get; set; }
}