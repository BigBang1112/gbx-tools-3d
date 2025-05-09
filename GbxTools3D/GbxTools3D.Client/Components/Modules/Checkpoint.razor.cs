using Microsoft.AspNetCore.Components;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

public partial class Checkpoint : ComponentBase
{
    public TimeInt32? Time { get; set; }
    public TimeInt32? Lap { get; set; }
    public TimeInt32? Diff { get; set; }

    public void Set(TimeInt32? time)
    {
        var hasChanged = Time != time;

        Time = time;

        if (hasChanged) // I dont even know if this helps or not
        {
            StateHasChanged();
        }
    }
}
