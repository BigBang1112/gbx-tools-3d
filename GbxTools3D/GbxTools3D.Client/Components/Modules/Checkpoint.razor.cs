using Microsoft.AspNetCore.Components;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

public partial class Checkpoint : ComponentBase
{
    private TimeInt32? time;

    public TimeInt32? Time
    {
        get => time;
        set
        {
            if (time == value) return; // I dont even know if this helps or not
            time = value;
            StateHasChanged();
        }
    }

    public TimeInt32? Lap { get; set; }
    public TimeInt32? Diff { get; set; }
}
