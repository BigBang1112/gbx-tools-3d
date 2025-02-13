using System.ComponentModel.DataAnnotations;
using GBX.NET;
using GBX.NET.Engines.Plug;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data.Entities;

[Index(nameof(GameVersion), nameof(Name))]
public sealed class Material
{
    public int Id { get; set; }
    
    public required GameVersion GameVersion { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }
    
    [StringLength(byte.MaxValue)]
    public required string Path { get; set; }
    
    public CPlugSurface.MaterialId SurfaceId { get; set; }
    public bool IsShader { get; set; }
    public Material? Shader { get; set; }
    
    public Dictionary<string, string> Textures { get; set; } = [];
}