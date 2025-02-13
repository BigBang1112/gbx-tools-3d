namespace GbxTools3D.Models;

internal sealed class CacheableHiddenData
{
    public required string Hash { get; init; }
    public required byte[] Data { get; init; }
    public required DateTime UpdatedAt { get; init; }
}