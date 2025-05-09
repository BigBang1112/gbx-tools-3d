using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace GbxTools3D.Client.Components.Modules;

public partial class MapInfo : ComponentBase
{
    private bool show = true;

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)", RegexOptions.IgnoreCase)]
    private static partial Regex MoodRegex();

    [Parameter, EditorRequired]
    public CGameCtnChallenge? Map { get; set; }

    public string? Mood => Map?.Decoration.Id is null ? null : MoodRegex().Match(Map.Decoration.Id).Value;

    private async Task CopyMapUidToClipboard()
    {
        if (!string.IsNullOrEmpty(Map?.MapUid))
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", Map.MapUid);
        }
    }
}
