using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using System.Collections.Immutable;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

public partial class InputList : ComponentBase
{
    private const string ModuleInputListHide = "ModuleInputListHide";
    private const string ModuleInputListOnlyRespawns = "ModuleInputListOnlyRespawns";
    private const string ModuleInputListSkipRespawns = "ModuleInputListSkipRespawns";

    private ElementReference inputList;
    private Virtualize<IInput>? virtualizeInputList;

    private bool show;
    private TimeInt32? currentInput;

    [Parameter, EditorRequired]
    public ImmutableList<IInput>? OverrideInputs { get; set; }

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<IInput> OnInputClick { get; set; }

    public static bool UseHundredths => false; // inputs can sometimes be millisecond-based in older TM games

    private bool onlyRespawns;
    private bool OnlyRespawns
    {
        get => onlyRespawns;
        set
        {
            onlyRespawns = value;
            SyncLocalStorage.SetItem(ModuleInputListOnlyRespawns, value);
            virtualizeInputList?.RefreshDataAsync();
            StateHasChanged();
        }
    }

    private bool skipRespawns;
    public bool SkipRespawns
    {
        get => skipRespawns;
        private set
        {
            skipRespawns = value;
            SyncLocalStorage.SetItem(ModuleInputListSkipRespawns, value);
        }
    }

    public TimeInt32? CurrentInput
    {
        get => currentInput;
        set
        {
            currentInput = value;
            StateHasChanged();
        }
    }

    public int CurrentInputIndex { get; set; }

    private ImmutableList<IInput>? inputs;
    private ImmutableList<IInput> Inputs => OverrideInputs ?? Ghost?.Inputs ?? Ghost?.PlayerInputs?.FirstOrDefault()?.Inputs ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        show = !await LocalStorage.GetItemAsync<bool>(ModuleInputListHide);
        onlyRespawns = await LocalStorage.GetItemAsync<bool>(ModuleInputListOnlyRespawns);
        skipRespawns = await LocalStorage.GetItemAsync<bool>(ModuleInputListSkipRespawns);
    }

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

        //await JS.InvokeVoidAsync("scrollToIndex", inputList, CurrentInputIndex);
    }

    private ValueTask<ItemsProviderResult<IInput>> LoadInputsAsync(ItemsProviderRequest request)
    {
        if (onlyRespawns)
        {
            var respawnInputs = Inputs.Where(x => x is Respawn { Pressed: true } or RespawnTM2020).ToList();
            var totalInputCount = respawnInputs.Count;
            var numInputs = Math.Min(request.Count, totalInputCount - request.StartIndex);
            var inputsSubset = respawnInputs.Skip(request.StartIndex)
                .Take(numInputs)
                .ToList();
            return ValueTask.FromResult(new ItemsProviderResult<IInput>(inputsSubset, totalInputCount));
        }
        else
        {
            var totalInputCount = Inputs.Count;
            var numInputs = Math.Min(request.Count, totalInputCount - request.StartIndex);
            var inputsSubset = Inputs.Skip(request.StartIndex)
                .Take(numInputs)
                .ToList();
            return ValueTask.FromResult(new ItemsProviderResult<IInput>(inputsSubset, totalInputCount));
        }
    }

    private async Task ToggleShowAsync()
    {
        show = !show;
        await LocalStorage.SetItemAsync(ModuleInputListHide, !show);
    }
}
