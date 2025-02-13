using System.ComponentModel.DataAnnotations;
using GBX.NET;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Hash), IsUnique = true)]
[Index(nameof(Path), nameof(GameVersion))]
public sealed class Texture
{
    public int Id { get; set; }

    public required GameVersion GameVersion { get; set; }

    [MinLength(64), MaxLength(64)]
    public required string Hash { get; set; }

    [MaxLength(16_777_215)]
    public required byte[] Data { get; set; }
    
    [StringLength(255)]
    public required string Path { get; set; }
    
    [StringLength(255)]
    public string? ImagePath { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}