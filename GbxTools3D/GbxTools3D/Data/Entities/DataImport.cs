using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public sealed class DataImport
{
    public int Id { get; set; }

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
