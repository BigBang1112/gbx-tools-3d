using GbxTools3D.Client.Enums;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class Controls : ComponentBase
{
    private bool show = true;
    private ReplayCameraType cameraType;
    private bool collisionsEnabled;

    [Parameter]
    public EventCallback<ReplayCameraType> OnCameraTypeChanged { get; set; }

    [Parameter]
    public EventCallback<bool> OnCollisionsToggled { get; set; }

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
}
