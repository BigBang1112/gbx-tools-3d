using GBX.NET.Engines.Game;
using Microsoft.AspNetCore.Components;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

public partial class CheckpointList : ComponentBase
{
    private record CheckpointInfo(int Index, CGameCtnGhost.Checkpoint Checkpoint, int Lap, bool IsFinish, bool HasBonusChanged);

    private bool show = true;

    [Parameter, EditorRequired]
    public CGameCtnGhost? Ghost { get; set; }

    [Parameter, EditorRequired]
    public int NumLaps { get; set; }

    [Parameter, EditorRequired]
    public EventCallback<CGameCtnGhost.Checkpoint> OnCheckpointClick { get; set; }

    public int CheckpointsPerLap => Ghost?.Checkpoints?.Length / NumLaps ?? 0;

    public TimeInt32? CurrentCheckpoint { get; private set; }

    public int CurrentCheckpointIndex { get; private set; } = -1;
    public int CurrentLapIndex => (CurrentCheckpointIndex + 1) / CheckpointsPerLap;

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

    public void SetCurrentCheckpoint(TimeInt32? time)
    {
        CurrentCheckpoint = time;
        StateHasChanged();
    }

    public void SetCurrentCheckpointIndex(int index)
    {
        CurrentCheckpointIndex = index;
        StateHasChanged();
    }
}
