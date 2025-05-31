using GbxTools3D.Enums;

namespace GbxTools3D.Client.Models;

public sealed record BlockClip
{
    public ClipDir Dir { get; set; }
    public required string Id { get; set; }
}