using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace GbxTools3D.Client.Components.Modules;

public partial class CheckpointList : ComponentBase
{
    private bool show = true;

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    [Parameter, EditorRequired]
    public int NumLaps { get; set; }

    public int CheckpointsPerLap => Ghost?.Checkpoints?.Length / NumLaps ?? 0;

    private CheckpointInfo[] Checkpoints => GetCheckpoints().ToArray();

    private IEnumerable<CheckpointInfo> GetCheckpoints()
    {
        var checkpoints = Ghost?.Checkpoints ?? [];

        var prevPoints = default(int?);
        var prevSpeed = default(float?);

        foreach (var (i, checkpoint) in checkpoints.Index())
        {
            if (checkpoint is null)
            {
                continue;
            }

            var isFinish = i == checkpoints.Length - 1;
            var hasBonusChanged = (prevPoints != checkpoint.StuntsScore && checkpoint.StuntsScore != 0) || prevSpeed != checkpoint.Speed;
            prevPoints = checkpoint.StuntsScore;
            prevSpeed = checkpoint.Speed;

            yield return new CheckpointInfo(i, checkpoint, i / CheckpointsPerLap, isFinish, hasBonusChanged);
        }
    }

    private record CheckpointInfo(int Index, CGameCtnGhost.Checkpoint Checkpoint, int Lap, bool IsFinish, bool HasBonusChanged);
}
