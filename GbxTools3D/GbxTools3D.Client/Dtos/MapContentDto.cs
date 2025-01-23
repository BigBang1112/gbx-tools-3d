namespace GbxTools3D.Client.Dtos;

public sealed class MapContentDto
{
    public MapInfoDto? Map { get; set; }
    public required byte[] Content { get; set; }
}
