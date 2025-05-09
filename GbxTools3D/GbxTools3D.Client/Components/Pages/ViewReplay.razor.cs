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

namespace GbxTools3D.Client.Components.Pages;

[SupportedOSPlatform("browser")]
public partial class ViewReplay : ComponentBase
{
    private View3D? view3d;
    private Playback? playback;
    private RenderInfo? renderInfo;
    private Checkpoint? checkpoint;
    private CheckpointList? checkpointList;

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

        var samples = ghost.SampleData.Samples;

        if (samples.Count == 0)
        {
            // case for input only replays
            return false;
        }

        var firstSample = samples[0];
        ghostSolid.Position = firstSample.Position;
        ghostSolid.RotationQuaternion = firstSample.Rotation;

        var count = samples.Count;
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
            times[i] = s.Time.TotalSeconds;
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

        var duration = samples.Last().Time.TotalSeconds;

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
    private TimeInt32? prevCheckpointPassedTime;

    [JSInvokable]
    public void UpdateTimeline(double timeIncludingRepeats)
    {
        var time = actions.GetValueOrDefault("Vehicle")?.GetPropertyAsDouble("time") ?? 0;
        playback?.SetTime(TimeSpan.FromSeconds(time));

        var checkpointPassedTime = default(TimeInt32?);

        // on maps with many checkpoints could be a performance hit
        foreach (var cp in CurrentGhost?.Checkpoints ?? [])
        {
            if (cp.Time is null)
            {
                continue;
            }

            var cpTime = cp.Time.Value.TotalSeconds;

            if (time >= cpTime - 0.001 && time < cpTime + 3)
            {
                checkpointPassedTime = cp.Time;

                if (checkpointPassedTime != prevCheckpointPassedTime)
                {
                    checkpoint?.Set(cp.Time);
                    checkpointList?.SetCurrentCheckpoint(cp.Time);
                    prevCheckpointPassedTime = checkpointPassedTime;
                }
            }

            // after passed checkpoint, dont check for the next checkpoints after 3 seconds
            if (checkpointPassedTime.HasValue && time > cpTime + 3)
            {
                break;
            }
        }

        if (checkpointPassedTime is null && prevCheckpointPassedTime is not null)
        {
            checkpoint?.Set(null);
            checkpointList?.SetCurrentCheckpoint(null);
            prevCheckpointPassedTime = null;
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

        playback?.SetTime(checkpoint.Time.Value);
        Animation.SetMixerTime(checkpoint.Time.Value.TotalSeconds);
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
