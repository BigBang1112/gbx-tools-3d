using GbxTools3D.Client.Enums;

namespace GbxTools3D.Client.Dtos;

public sealed class MapInfoDto
{
    public required string UploaderId { get; set; }
    public required string UploaderName { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required UnlimiterVersion? Unlimiter { get; set; }
    public required Guid? OnlineMapId { get; set; }
}
