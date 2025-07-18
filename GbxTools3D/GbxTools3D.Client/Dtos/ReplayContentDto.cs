namespace GbxTools3D.Client.Dtos;

public sealed class ReplayContentDto
{
    public MapInfoDto? Map { get; set; }
    public ReplayInfoDto? Replay { get; set; }
    public required byte[] Content { get; set; }
}
