using System.ComponentModel.DataAnnotations;

namespace GbxTools3D.Data.Entities;

public class Mesh
{
    public int Id { get; set; }

    [MinLength(64), MaxLength(64)]
    public required string Hash { get; set; }

    [MaxLength(16_777_215)]
    public required byte[] Data { get; set; }

    [MaxLength(16_777_215)]
    public byte[]? DataLQ { get; set; }

    public override string ToString()
    {
        return $"{Hash} ({Data.Length} b{(DataLQ is null ? "" : $", LQ: {DataLQ.Length}")})";
    }
}
