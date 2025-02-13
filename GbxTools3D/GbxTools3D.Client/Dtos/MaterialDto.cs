using GBX.NET.Engines.Plug;

namespace GbxTools3D.Client.Dtos;

public sealed class MaterialDto
{
    public CPlugSurface.MaterialId SurfaceId { get; set; }
    public bool IsShader { get; set; }
    public string? Shader { get; set; }
    public Dictionary<string, string>? Textures { get; set; }
}