namespace GbxTools3D.Client.Dtos;

public sealed class BlockVariantDto
{
    public byte Variant { get; set; }
    public byte SubVariant { get; set; }
    public List<ObjectLinkDto>? ObjectLinks { get; set; }
}