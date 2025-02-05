using GbxTools3D.Enums;

namespace GbxTools3D.Client.Models;

public sealed class BlockClip
{
    public ClipDir Dir { get; set; }
    public required string Id { get; set; }
}