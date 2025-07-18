using GBX.NET;
using GBX.NET.Engines.Game;
using GbxTools3D.Client.EventArgs;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.RegularExpressions;

namespace GbxTools3D.Client.Components.Modules;

public partial class MapInfo : ComponentBase
{
    private bool show = true;

    private readonly string[] extensions = ["Challenge.Gbx", "Map.Gbx"];

    [GeneratedRegex(@"(Sunrise|Day|Sunset|Night)", RegexOptions.IgnoreCase)]
    private static partial Regex MoodRegex();

    [Parameter, EditorRequired]
    public CGameCtnChallenge? Map { get; set; }

    [Parameter]
    public EventCallback<CGameCtnChallenge?> MapUploaded { get; set; }

    [Parameter]
    public bool Uploadable { get; set; }

    public string? Mood
    {
        get
        {
            if (Map?.Decoration is null)
            {
                return null;
            }

            var match = MoodRegex().Match(Map.Decoration.Id);

            if (match.Success)
            {
                return match.Value;
            }

            return "Day";
        }
    }

    private async Task CopyMapUidToClipboard()
    {
        if (!string.IsNullOrEmpty(Map?.MapUid))
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", Map.MapUid);
        }
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        if (gbx is Gbx<CGameCtnChallenge> map)
        {
            Map = map.Node;
            await MapUploaded.InvokeAsync(Map);
        }
    }
}
