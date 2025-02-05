namespace GbxTools3D.Client.Dtos;

public sealed class BlockVariantDto
{
    public required byte Variant { get; set; }
    public required byte SubVariant { get; set; }
    public List<ObjectLinkDto>? ObjectLinks { get; set; }
}