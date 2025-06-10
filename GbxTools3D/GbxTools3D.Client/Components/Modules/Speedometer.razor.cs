using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class Speedometer : ComponentBase
{
    private bool show = true;

    private int? Gear { get; set; }
    private float Speed { get; set; }
    private float RPM { get; set; }

    public void SetSpeed(float speed)
    {
        if (Speed == speed)
        {
            return;
        }

        Speed = speed;
        StateHasChanged();
    }

    public void SetGear(int gear)
    {
        if (Gear == gear)
        {
            return;
        }

        Gear = gear;
        StateHasChanged();
    }

    public void SetRPM(float rpm)
    {
        if (RPM == rpm)
        {
            return;
        }

        RPM = rpm;
        StateHasChanged();
    }
}
