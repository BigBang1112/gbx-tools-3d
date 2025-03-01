namespace GbxTools3D.Client.Dtos;

public sealed class CollectionDto
{
    public required string Name { get; set; }
    public string? DisplayName { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SquareHeight { get; set; }
    public int SquareSize { get; set; }
    public required IdentDto Vehicle { get; set; }
    public string? DefaultZoneBlock { get; set; }
    public int SortIndex { get; set; }

    public bool HasBlocks { get; set; }
    public bool HasDecorations { get; set; }
    public bool HasVehicles { get; set; }
    public bool HasItems { get; set; }
    public bool HasMacroblocks { get; set; }
}
