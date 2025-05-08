using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GbxTools3D.Client.Components.Modules;

public partial class MapInfo : ComponentBase
{
    private bool show = true;

    [Parameter, EditorRequired]
    public CGameCtnChallenge? Map { get; set; }

    private async Task CopyMapUidToClipboard()
    {
        if (!string.IsNullOrEmpty(Map?.MapUid))
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", Map.MapUid);
        }
    }
}
