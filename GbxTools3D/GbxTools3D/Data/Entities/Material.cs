﻿using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using GBX.NET;
using GBX.NET.Engines.Plug;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(GameVersion))]
public sealed class Material
{
    public int Id { get; set; }
    
    public required GameVersion GameVersion { get; set; }
    
    // NOT unique, even within GameVersion
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }
    
    public CPlugSurface.MaterialId SurfaceId { get; set; }
    public bool IsShader { get; set; }
    public Material? Shader { get; set; }

    public TerrainModifier? Modifier { get; set; }

    public ImmutableDictionary<string, string> Textures { get; set; } = ImmutableDictionary<string, string>.Empty;
}