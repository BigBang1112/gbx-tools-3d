using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Collections.Immutable;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

public partial class InputList : ComponentBase
{
    private Virtualize<IInput>? virtualizeInputList;

    private bool show = true;
    private bool onlyRespawns = false;
    private TimeInt32? currentInput;

    [Parameter, EditorRequired]
    public CGameCtnReplayRecord? Replay { get; set; }

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<IInput> OnInputClick { get; set; }

    public TimeInt32? CurrentInput
    {
        get => currentInput;
        set
        {
            currentInput = value;
            StateHasChanged();
        }
    }

    private ImmutableList<IInput>? inputs;
    private ImmutableList<IInput> Inputs => Replay?.Inputs ?? Ghost?.Inputs ?? Ghost?.PlayerInputs?.FirstOrDefault()?.Inputs ?? [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Inputs != inputs)
        {
            if (virtualizeInputList is not null)
            {
                await virtualizeInputList.RefreshDataAsync();
            }

            inputs = Inputs;
        }
    }

    private ValueTask<ItemsProviderResult<IInput>> LoadInputsAsync(ItemsProviderRequest request)
    {
        var numInputs = Math.Min(request.Count, Inputs.Count - request.StartIndex);
        var inputs = Inputs.Where(x => !onlyRespawns || x is Respawn { Pressed: true } or RespawnTM2020).Skip(request.StartIndex).Take(numInputs);

        return ValueTask.FromResult(new ItemsProviderResult<IInput>(inputs, Inputs.Count));
    }
}
