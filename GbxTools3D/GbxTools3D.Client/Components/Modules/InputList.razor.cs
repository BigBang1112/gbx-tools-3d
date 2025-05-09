using GBX.NET.Engines.Game;
using GBX.NET.Inputs;
using Microsoft.AspNetCore.Components;
using System.Collections.Immutable;

namespace GbxTools3D.Client.Components.Modules;

public partial class InputList : ComponentBase
{
    private bool show = true;
    private bool onlyRespawns = false;

    [Parameter, EditorRequired]
    public CGameCtnReplayRecord? Replay { get; set; }

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    private ImmutableList<IInput> Inputs => Replay?.Inputs ?? Ghost?.Inputs ?? Ghost?.PlayerInputs?.FirstOrDefault()?.Inputs ?? [];
}
