using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class GhostInfo : ComponentBase
{
    private bool show = true;

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }
}
