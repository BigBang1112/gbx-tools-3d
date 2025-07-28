namespace GbxTools3D.External.TMX;

internal sealed class TmxReplayInfo
{
    public required MxUser User { get; set; }
    public int? Position { get; set; }
    public required DateTime ReplayAt { get; set; }
}
