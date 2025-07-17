namespace GbxTools3D.External.MX;

internal sealed class MxMapInfo
{
    public required ulong MapId { get; set; }
    public required string Name { get; set; }
    public required MxUser Uploader { get; set; }
    public required MxAuthor[] Authors { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required Guid? OnlineMapId { get; set; }
}
