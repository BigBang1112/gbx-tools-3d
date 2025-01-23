namespace GbxTools3D.Client.Dtos;

public sealed class ItemContentDto
{
    public ItemInfoDto? Item { get; set; }
    public required byte[] Content { get; set; }
}
