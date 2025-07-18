using GbxTools3D.Client.Enums;

namespace GbxTools3D.Client.Dtos;

public sealed class MapInfoDto
{
    public required string Name { get; set; }
    public required string UploaderId { get; set; }
    public required string UploaderName { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public UnlimiterVersion? Unlimiter { get; set; }
    public Guid? OnlineMapId { get; set; }
    public ulong? MxId { get; set; }
}
