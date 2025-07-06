namespace GbxTools3D.External.MX;

internal sealed class IxItemInfo
{
    public required string Name { get; set; }
    public required ulong UserID { get; set; }
    public required string Username { get; set; }
    public required DateTime Updated { get; set; }
    public required int Score { get; set; }
    public required ulong SetID { get; set; }
    public required string? SetName { get; set; }
    public required string? Directory { get; set; }
    public required string? ZipIndex { get; set; }
}
