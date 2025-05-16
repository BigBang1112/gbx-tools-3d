using GbxTools3D.Client.Models;

namespace GbxTools3D.Client.Dtos;

public sealed class BlockInfoDto
{
    public required string Name { get; set; }
    public required BlockUnit[] AirUnits { get; set; }
    public required BlockUnit[] GroundUnits { get; set; }
    public bool HasAirHelper { get; set; }
    public bool HasGroundHelper { get; set; }
    public bool HasConstructionModeHelper { get; set; }
    public required List<BlockVariantDto> AirVariants { get; set; }
    public required List<BlockVariantDto> GroundVariants { get; set; }
    public byte? Height { get; set; }
    public bool IsDefaultZone { get; set; }
    public bool HasIcon { get; set; }
    public bool IsRoad { get; set; }
}