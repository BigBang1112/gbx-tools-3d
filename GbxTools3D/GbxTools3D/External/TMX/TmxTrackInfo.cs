﻿using GbxTools3D.Client.Enums;

namespace GbxTools3D.External.TMX;

internal sealed class TmxTrackInfo
{
    public required ulong TrackId { get; set; }
    public required string TrackName { get; set; }
    public required MxUser Uploader { get; set; }
    public required MxAuthor[] Authors { get; set; }
    public required DateTime UpdatedAt { get; set; }
    public required UnlimiterVersion? UnlimiterVersion { get; set; }
}
