using GbxTools3D.Client.Enums;

namespace GbxTools3D.Client.Models;

public sealed class PlaybackMarker
{
    public required TimeSpan Time { get; set; }
    public PlaybackMarkerType Type { get; set; }
}
