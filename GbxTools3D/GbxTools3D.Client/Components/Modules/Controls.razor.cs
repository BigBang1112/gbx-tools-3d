using GbxTools3D.Client.Enums;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class Controls : ComponentBase
{
    private const string ModuleControlsHide = "ModuleControlsHide";

    private bool show;
    private ReplayCameraType cameraType;
    private bool collisionsEnabled;

    [Parameter]
    public EventCallback<ReplayCameraType> OnCameraTypeChanged { get; set; }

    [Parameter]
    public EventCallback<bool> OnCollisionsToggled { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (RendererInfo.IsInteractive)
        {
            show = !await LocalStorage.GetItemAsync<bool>(ModuleControlsHide);
        }
    }

    private async Task SwitchCameraTypeAsync(ReplayCameraType type)
    {
        cameraType = type;
        await OnCameraTypeChanged.InvokeAsync(type);
    }

    private async Task ToggleCollisionsAsync()
    {
        collisionsEnabled = !collisionsEnabled;
        await OnCollisionsToggled.InvokeAsync(collisionsEnabled);
    }

    private async Task ToggleShowAsync()
    {
        show = !show;
        await LocalStorage.SetItemAsync(ModuleControlsHide, !show);
    }
}
