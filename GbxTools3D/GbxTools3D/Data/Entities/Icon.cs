using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbxTools3D.Data.Entities;

public sealed class Icon
{
    public int Id { get; set; }
    
    [StringLength(byte.MaxValue)]
    public string? TexturePath { get; set; }
    
    [StringLength(byte.MaxValue)]
    public string? ImagePath { get; set; }

    [Column(TypeName = "blob")]
    public byte[]? Data { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}