using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

[Index(nameof(Hash), IsUnique = true)]
public class Mesh
{
    public int Id { get; set; }

    [MinLength(64), MaxLength(64)]
    public required string Hash { get; set; }

    [MaxLength(16_777_215)]
    public required byte[] Data { get; set; }

    [MaxLength(16_777_215)]
    public byte[]? DataLQ { get; set; }

    [MaxLength(16_777_215)]
    public byte[]? DataELQ { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return $"{Hash} ({Data.Length} b{(DataLQ is null ? "" : $", LQ: {DataLQ.Length}")})";
    }
}
