using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class Speedometer : ComponentBase
{
    private const string ModuleSpeedometerHide = "ModuleSpeedometerHide";

    private bool show;

    private int? gear;
    private float speed;
    private float rpm;

    public int? Gear
    {
        get => gear;
        set 
        {
            if (gear == value)
            {
                return;
            }
            gear = value;
            StateHasChanged();
        }
    }

    public float Speed
    {
        get => speed;
        set
        {
            if (speed == value)
            {
                return;
            }
            speed = value;
            StateHasChanged();
        }
    }

    public float RPM
    {
        get => rpm;
        set
        {
            if (rpm == value)
            {
                return;
            }
            rpm = value;
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (RendererInfo.IsInteractive)
        {
            show = !await LocalStorage.GetItemAsync<bool>(ModuleSpeedometerHide);
        }
    }

    private async Task ToggleShowAsync()
    {
        show = !show;
        await LocalStorage.SetItemAsync(ModuleSpeedometerHide, !show);
    }
}
