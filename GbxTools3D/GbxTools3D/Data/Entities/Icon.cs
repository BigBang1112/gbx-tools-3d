using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class Icon
{
    public int Id { get; set; }
    
    [StringLength(255)]
    public string? TexturePath { get; set; }
    
    [StringLength(255)]
    public string? ImagePath { get; set; }

    [MaxLength(16_777_215)]
    public byte[]? Data { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}