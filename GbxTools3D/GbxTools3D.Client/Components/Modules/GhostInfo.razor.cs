using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class GhostInfo : ComponentBase
{
    private const string ModuleGhostInfoHide = "ModuleGhostInfoHide";
    private const string ModuleGhostInfoMode = "ModuleGhostInfoMode";

    private bool show;
    private MenuType mode;
    private CGameGhost.Data.Sample? currentSample;
    private CGameGhost.Data.Sample? currentSampleInterpolated;

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    [Parameter]
    public bool UseHundredths { get; set; }

    private (string, string)? VersionName => Ghost?.SampleData?.Version switch
    {
        1 or 2 => ("SVehicleSimpleState_ReplayAfter211003", "2003-10-21"),
        4 => ("SVehicleSimpleState_ReplayAfter040104", "2004-01-04"),
        5 or 7 => ("SVehicleSimpleState_ReplayAfter100205", "2005-02-10"),
        8 or 9 => ("SVehicleSimpleState_ReplayAfter081205", "2005-12-08"),
        10 or 11 or 12 => ("SVehicleSimpleState_ReplayAfter230211", "2011-02-23"),
        13 => ("SVehicleSimpleNetState", "network"),
        14 or 15 => ("SVehicleSimpleState_ReplayAfter270115", "2015-01-27"),
        16 => ("SVehicleSimpleState_ReplayAfter160216", "2016-02-16"),
        17 => ("SVehicleSimpleState_ReplayAfter100117", "2017-01-10"),
        18 or 19 => ("SVehicleSimpleState_ReplayAfter111217", "2017-12-11"),
        20 => ("SVehicleSimpleState_ReplayAfter2018_03_09", "2018-03-09"),
        _ => null
    };

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        show = !await LocalStorage.GetItemAsync<bool>(ModuleGhostInfoHide);
        mode = await LocalStorage.GetItemAsync<MenuType>(ModuleGhostInfoMode);
    }

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

    private static string InterpolateDirtBlend(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        int rStart = 255, gStart = 255, bStart = 255;      // #FFFFFF
        int rEnd = 244, gEnd = 164, bEnd = 96;             // #F4A460

        int r = (int)(rStart + (rEnd - rStart) * t);
        int g = (int)(gStart + (gEnd - gStart) * t);
        int b = (int)(bStart + (bEnd - bStart) * t);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private async Task ToggleShowAsync()
    {
        show = !show;
        await LocalStorage.SetItemAsync(ModuleGhostInfoHide, !show);
    }

    private async Task SwitchModeAsync(MenuType newMode)
    {
        if (mode == newMode)
        {
            return;
        }

        mode = newMode;

        await LocalStorage.SetItemAsync(ModuleGhostInfoMode, mode);
    }
}
