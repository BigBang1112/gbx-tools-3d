using GBX.NET.Engines.Game;
using GBX.NET.Engines.Scene;
using GBX.NET;
using GbxTools3D.Client.EventArgs;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Microsoft.JSInterop;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Enums;
using GbxTools3D.Client.Components.Modules;
using GbxTools3D.Client.Extensions;
using TmEssentials;
using System.Text;

namespace GbxTools3D.Client.Components.Pages;

[SupportedOSPlatform("browser")]
public partial class ViewReplay : ComponentBase
{
    private View3D? view3d;
    private Playback? playback;
    private RenderInfo? renderInfo;
    private Checkpoint? checkpoint;
    private CheckpointList? checkpointList;
    private Speedometer? speedometer;
    private GhostInfo? ghostInfo;

    private readonly Dictionary<string, JSObject> actions = [];

    private readonly string[] wheelNames = ["FLWheel", "FRWheel", "RLWheel", "RRWheel"];
    private readonly string[] guardNames = ["FLGuard", "FRGuard", "RLHub", "RRHub"];

    private readonly string[] extensions = ["Replay.Gbx"];

    [SupplyParameterFromQuery(Name = "tmx")]
    private string? TmxSite { get; set; }

    [SupplyParameterFromQuery(Name = "mx")]
    private string? MxSite { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    private string? MapId { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(TmxSite) && string.IsNullOrEmpty(MxSite);

    public CGameCtnReplayRecord? Replay { get; set; }
    public CGameCtnGhost? CurrentGhost { get; set; }

    public RenderDetails? RenderDetails { get; set; }

    private string selectedExternal = "tmx";
    private string selectedTmx = "tmnf";
    private string selectedMx = "tm2020";
    private string externalId = string.Empty;

    private string GetPlaybackDescription()
    {
        if (CurrentGhost is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (CurrentGhost.RaceTime is not null)
        {
            sb.Append(CurrentGhost.RaceTime);
            sb.Append(" by ");
        }

        sb.Append(CurrentGhost.GhostNickname ?? CurrentGhost.GhostLogin);
        return sb.ToString();
    }

    protected override async Task OnInitializedAsync()
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (IsDragAndDrop)
        {
            Replay = GbxService.SelectedReplay?.Node;
            return;
        }

        string endpoint;
        if (!string.IsNullOrEmpty(TmxSite))
        {
            endpoint = $"/api/replay/tmx/{TmxSite}/{MapId}";
        }
        else if (!string.IsNullOrEmpty(MxSite))
        {
            endpoint = $"/api/replay/mx/{MxSite}/{MapId}";
        }
        else
        {
            throw new Exception();
        }

        using var response = await Http.GetAsync(endpoint);
        var content = await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ReplayContentDto);

        if (content is null)
        {
            return;
        }

        await using var ms = new MemoryStream(content.Content);
        Replay = Gbx.ParseNode<CGameCtnReplayRecord>(ms);
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        await using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        if (gbx is Gbx<CGameCtnReplayRecord> replay)
        {
            GbxService.Add(replay);
            Replay = replay.Node;
        }
    }

    private async Task BeforeMapLoadAsync()
    {
        var animationModule = await JS.InvokeAsync<IJSObjectReference>("import", $"./js/animation.js");
        await animationModule.InvokeVoidAsync("registerDotNet", DotNetObjectReference.Create(this));

        await TryLoadReplayAsync();
    }

    private async ValueTask<bool> TryLoadReplayAsync()
    {
        if (view3d is null || Replay is null)
        {
            return false;
        }

        var ghost = Replay.GetGhosts().FirstOrDefault();

        CurrentGhost = ghost;

        if (ghost?.SampleData is null)
        {
            return false;
        }

        var ghostSolid = await view3d.LoadGhostAsync(ghost);

        if (ghostSolid is null)
        {
            return false;
        }

        var isOldSampleChunk = ghost.Chunks.Get<CGameGhost.Chunk0303F003>() is not null;
        var samples = isOldSampleChunk
            ? ghost.SampleData.Samples.Skip(1).ToArray().AsSpan() // weird old tm stuff
            : ghost.SampleData.Samples.ToArray().AsSpan();

        if (samples.Length == 0)
        {
            // case for input only replays
            return false;
        }

        var firstSample = samples[0];
        ghostSolid.Position = firstSample.Position;
        ghostSolid.RotationQuaternion = firstSample.Rotation;

        var count = samples.Length;
        var times = new double[count];
        var positions = new double[count * 3];
        var rotations = new double[count * 4];

        // Dictionaries for per-part data
        var partData = new Dictionary<string, double[]>();
        // Initialize arrays for wheels and guards
        foreach (var wheel in wheelNames)
        {
            partData[$"{wheel}_rot"] = new double[count];
            partData[$"{wheel}_steer"] = new double[count];
            partData[$"{wheel}_dampen"] = new double[count];
        }
        foreach (var guard in guardNames)
        {
            partData[$"{guard}_steer"] = new double[count];
            partData[$"{guard}_dampen"] = new double[count];
        }

        for (int i = 0; i < count; i++)
        {
            var s = (CSceneVehicleCar.Sample)samples[i];
            times[i] = s.Time.TotalSeconds - (isOldSampleChunk ? 0.1 : 0);
            positions[i * 3] = s.Position.X;
            positions[i * 3 + 1] = s.Position.Y;
            positions[i * 3 + 2] = s.Position.Z;
            rotations[i * 4] = s.Rotation.X;
            rotations[i * 4 + 1] = s.Rotation.Y;
            rotations[i * 4 + 2] = s.Rotation.Z;
            rotations[i * 4 + 3] = s.Rotation.W;

            partData["FLWheel_rot"][i] = s.FLWheelRotation;
            partData["FRWheel_rot"][i] = s.FRWheelRotation;
            partData["RLWheel_rot"][i] = s.RLWheelRotation;
            partData["RRWheel_rot"][i] = s.RRWheelRotation;

            partData["FLWheel_steer"][i] = s.SteerFront;
            partData["FRWheel_steer"][i] = s.SteerFront;

            partData["FLWheel_dampen"][i] = s.FLDampenLen;
            partData["FRWheel_dampen"][i] = s.FRDampenLen;
            partData["RLWheel_dampen"][i] = s.RLDampenLen;
            partData["RRWheel_dampen"][i] = s.RRDampenLen;

            partData["FLGuard_steer"][i] = s.SteerFront;
            partData["FRGuard_steer"][i] = s.SteerFront;
            partData["FLGuard_dampen"][i] = s.FLDampenLen;
            partData["FRGuard_dampen"][i] = s.FRDampenLen;
            partData["RLHub_dampen"][i] = s.RLDampenLen;
            partData["RRHub_dampen"][i] = s.RRDampenLen;
        }

        var duration = samples[^1].Time.TotalSeconds - (isOldSampleChunk ? 0.1 : 0);

        // extra duration in case the last checkpoint is later than the last sample
        if (ghost.Checkpoints?.LastOrDefault()?.Time?.TotalSeconds > duration)
        {
            duration += ghost.SampleData.SamplePeriod.TotalSeconds;
        }

        var positionTrack = Animation.CreatePositionTrack(times, positions);
        var rotationTrack = Animation.CreateQuaternionTrack(times, rotations);

        // Setup mixer and playback
        Animation.CreateMixer(ghostSolid.Object);
        playback?.SetDuration(TimeSpan.FromSeconds(duration));

        actions["Vehicle"] = Animation.CreateAction(Animation.CreateClip("Vehicle", duration, [positionTrack, rotationTrack]));

        foreach (var wheel in wheelNames)
        {
            var node = ghostSolid.GetObjectByName("1" + wheel);
            if (node is null) continue;

            Solid.ReorderEuler(node);

            // Rotation
            var rotTrack = Animation.CreateRotationXTrack(times, partData[$"{wheel}_rot"]);
            actions[$"{wheel}Rotation"] = Animation.CreateAction(
                Animation.CreateClip($"{wheel}Rotation", duration, [rotTrack]), node);

            // Steer
            var steerTrack = Animation.CreateRotationYTrack(times, partData[$"{wheel}_steer"]);
            actions[$"{wheel}Steer"] = Animation.CreateAction(
                Animation.CreateClip($"{wheel}Steer", duration, [steerTrack]), node);

            // Dampen (as relative position)
            var dampenTrack = Animation.CreateRelativePositionYTrack(times, partData[$"{wheel}_dampen"], node);
            actions[$"{wheel}Dampen"] = Animation.CreateAction(
                Animation.CreateClip($"{wheel}Dampen", duration, [dampenTrack]), node);
        }

        foreach (var guard in guardNames)
        {
            var node = ghostSolid.GetObjectByName("1" + guard);
            if (node is null) continue;

            Solid.ReorderEuler(node);

            var steerTrack = Animation.CreateRotationYTrack(times, partData[$"{guard}_steer"]);
            actions[$"{guard}Steer"] = Animation.CreateAction(
                Animation.CreateClip($"{guard}Steer", duration, [steerTrack]), node);

            var dampenTrack = Animation.CreateRelativePositionYTrack(times, partData[$"{guard}_dampen"], node);
            actions[$"{guard}Dampen"] = Animation.CreateAction(
                Animation.CreateClip($"{guard}Dampen", duration, [dampenTrack]), node);
        }

        var checkpoints = ghost.Checkpoints ?? [];
        var numLaps = ghost.GetNumberOfLaps(Replay?.Challenge) ?? 1;
        var perLap = checkpoints.Length / numLaps;

        playback?.SetMarkers(checkpoints.Where(c => c.Time.HasValue).Select((c, i) =>
            new PlaybackMarker
            {
                Time = c.Time!.Value,
                Type = c == checkpoints.Last()
                    ? PlaybackMarkerType.Finish
                    : (((i + 1) % perLap == 0)
                        ? PlaybackMarkerType.Multilap
                        : PlaybackMarkerType.Checkpoint)
            }).ToList());

        StateHasChanged();

        return true;
    }

    private double prevTime;
    private int prevSampleIndex = -1;
    private int prevCheckpointPassedIndex = -1;
    private TimeInt32? prevCheckpointPassedTime;

    [JSInvokable]
    public void UpdateTimeline(double timeIncludingRepeats)
    {
        var time = actions.GetValueOrDefault("Vehicle")?.GetPropertyAsDouble("time") ?? 0;
        playback?.SetTime(TimeSpan.FromSeconds(time));

        if (CurrentGhost is not null)
        {
            // Ensure checkpoints are sorted by Time
            var checkpoints = CurrentGhost.Checkpoints ?? [];
            if (checkpoints.Length == 0)
            {
                return;
            }

            int left = 0, right = checkpoints.Length - 1, mid = -1;
            TimeInt32? checkpointPassedTime = null;

            // Binary search for the first checkpoint within the range
            while (left <= right)
            {
                mid = (left + right) / 2;
                var cp = checkpoints[mid];
                if (cp.Time is null)
                {
                    continue;
                }

                var cpTime = cp.Time.Value.TotalSeconds;
                var nextCpTime = (mid + 1 < checkpoints.Length && checkpoints[mid + 1].Time.HasValue)
                    ? checkpoints[mid + 1].Time!.Value.TotalSeconds
                    : double.MaxValue; // Handle last checkpoint case

                if (time >= cpTime - 0.001 && time < nextCpTime)
                {
                    if (mid != prevCheckpointPassedIndex)
                    {
                        checkpointList?.SetCurrentCheckpoint(cp.Time);
                        checkpointList?.SetCurrentCheckpointIndex(nextCpTime == double.MaxValue ? (mid - 1) : mid);
                        prevCheckpointPassedIndex = mid;
                    }

                    if (time < cpTime + 3)
                    {
                        checkpointPassedTime = cp.Time;

                        if (checkpointPassedTime != prevCheckpointPassedTime)
                        {
                            checkpoint?.Set(cp.Time);
                            prevCheckpointPassedTime = checkpointPassedTime;
                        }
                    }
                    break;
                }
                else if (time < cpTime - 0.001)
                {
                    right = mid - 1;

                    if (right == -1)
                    {
                        // No checkpoint found before this time, reset
                        prevCheckpointPassedIndex = -1;
                        checkpointList?.SetCurrentCheckpoint(null);
                        checkpointList?.SetCurrentCheckpointIndex(-1);
                        checkpoint?.Set(null);
                        prevCheckpointPassedTime = null;
                    }
                }
                else
                {
                    left = mid + 1;
                }
            }

            // Handle case where no checkpoint is passed
            if (checkpointPassedTime is null && prevCheckpointPassedTime is not null)
            {
                checkpoint?.Set(null);
                prevCheckpointPassedTime = null;
            }

            if (CurrentGhost.SampleData is not null)
            {
                var sampleIndex = (int)(time / CurrentGhost.SampleData.SamplePeriod.TotalSeconds);
                var nextSampleIndex = sampleIndex + 1;

                if (nextSampleIndex >= CurrentGhost.SampleData.Samples.Count)
                {
                    nextSampleIndex = CurrentGhost.SampleData.Samples.Count - 1;
                }

                var currentSample = CurrentGhost.SampleData.Samples[sampleIndex];
                var nextSample = CurrentGhost.SampleData.Samples[nextSampleIndex];
                var lerpFactor = (time - currentSample.Time.TotalSeconds) / (nextSample.Time.TotalSeconds - currentSample.Time.TotalSeconds);

                if (currentSample is CSceneVehicleCar.Sample currentCarSampleToLerp && nextSample is CSceneVehicleCar.Sample nextCarSampleToLerp)
                {
                    speedometer?.SetRPM(AdditionalMath.Lerp(currentCarSampleToLerp.RPM, nextCarSampleToLerp.RPM, (float)lerpFactor));
                    speedometer?.SetSpeed(AdditionalMath.Lerp(currentCarSampleToLerp.VelocitySpeed, nextCarSampleToLerp.VelocitySpeed, (float)lerpFactor));
                }

                // only per sample update, not interpolated
                if (sampleIndex != prevSampleIndex)
                {
                    //speedometer?.SetSpeed((int)currentSample.VelocitySpeed);
                    ghostInfo?.SetCurrentSample(currentSample);

                    if (currentSample is CSceneVehicleCar.Sample currentCarSample)
                    {
                        //speedometer?.SetRPM(carSample.RPM);
                    }

                    prevSampleIndex = sampleIndex;
                }
            }
        }

        prevTime = time;
    }

    private void OnRenderDetails(RenderDetails details)
    {
        RenderDetails = details;
        renderInfo?.Update();
    }

    private void OnCheckpointListItemClick(CGameCtnGhost.Checkpoint checkpoint)
    {
        if (checkpoint.Time is null)
        {
            return;
        }

        playback?.Seek(checkpoint.Time.Value, seekPause: false);
    }

    public ValueTask DisposeAsync()
    {
        if (RendererInfo.IsInteractive)
        {
            Animation.SetMixerTimeScale(1, isPaused: false);
        }

        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }

    private void Play(bool firstPlay)
    {
        if (firstPlay)
        {
            foreach (var action in actions.Values)
            {
                Animation.PlayAction(action);
            }
        }

        Animation.PlayMixer();
    }

    private void Pause()
    {
        Animation.PauseMixer();
    }

    private void Rewind()
    {
        Animation.SetMixerTime(0);
    }

    private void SetSpeed(float speed)
    {
        Animation.SetMixerTimeScale(speed, playback?.IsPaused ?? true);
    }
}
