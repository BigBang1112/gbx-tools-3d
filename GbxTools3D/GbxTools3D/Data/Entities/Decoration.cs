namespace GbxTools3D.Data.Entities;

public sealed class Decoration
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int CollectionId { get; set; }
    public required Collection Collection { get; set; }

    public int DecorationSizeId { get; set; }
    public required DecorationSize DecorationSize { get; set; }
}