using GBX.NET.Engines.Plug;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Dtos;

public sealed class MaterialDto
{
    public CPlugSurface.MaterialId SurfaceId { get; set; }
    public bool IsShader { get; set; }
    public string? Shader { get; set; }
    public ImmutableDictionary<string, string>? Textures { get; set; }
    public ImmutableDictionary<string, MaterialDto>? Modifiers { get; set; }
}