using GbxTools3D.Client.Models;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class RenderInfo : ComponentBase
{
    private bool show = true;

    [Parameter, EditorRequired]
    public RenderDetails? RenderDetails { get; set; }

    public void Update()
    {
        StateHasChanged();
    }
}
