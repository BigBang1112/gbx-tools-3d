using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class GhostInfo : ComponentBase
{
    private bool show = true;
    private MenuType mode;
    private CGameGhost.Data.Sample? currentSample;
    private CGameGhost.Data.Sample? currentSampleInterpolated;

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    public void SetCurrentSample(CGameGhost.Data.Sample? sample)
    {
        if (currentSample == sample)
        {
            return;
        }
        currentSample = sample;
        StateHasChanged();
    }

    private enum MenuType
    {
        Samples,
        Details
    }
}
