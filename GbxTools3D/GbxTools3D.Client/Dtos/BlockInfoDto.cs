﻿using GbxTools3D.Client.Models;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Dtos;

public sealed class BlockInfoDto
{
    public required string Name { get; set; }
    public required ImmutableArray<BlockUnit> AirUnits { get; set; }
    public required ImmutableArray<BlockUnit> GroundUnits { get; set; }
    public bool HasAirHelper { get; set; }
    public bool HasGroundHelper { get; set; }
    public bool HasConstructionModeHelper { get; set; }
    public bool HasAirWaypoint { get; set; }
    public bool HasGroundWaypoint { get; set; }
    public required List<BlockVariantDto> AirVariants { get; set; }
    public required List<BlockVariantDto> GroundVariants { get; set; }
    public byte? Height { get; set; }
    public bool IsDefaultZone { get; set; }
    public bool HasIcon { get; set; }
    public bool IsRoad { get; set; }
    public string? PylonName { get; set; }
    public string? TerrainModifier { get; set; }
}