namespace GbxTools3D.Client.Models;

public readonly record struct UniqueVariant(string Name, bool IsGround, int Variant, int SubVariant, string? TerrainModifier);