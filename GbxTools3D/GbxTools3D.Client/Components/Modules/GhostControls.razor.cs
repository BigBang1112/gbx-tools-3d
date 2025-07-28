using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Scene;
using GBX.NET.Inputs;
using GbxTools3D.Client.Enums;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Immutable;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using TmEssentials;

namespace GbxTools3D.Client.Components.Modules;

[SupportedOSPlatform("browser")]
public partial class GhostControls : ComponentBase
{
    private Playback? playback;
    private RenderInfo? renderInfo;
    private Checkpoint? checkpoint;
    private CheckpointList? checkpointList;
    private InputList? inputList;
    private Speedometer? speedometer;
    private GhostInfo? ghostInfo;

    private Solid? ghostSolid;
    private Solid? ghostCollisionSolid;

    private readonly string[] wheelNames = ["FLWheel", "FRWheel", "RLWheel", "RRWheel"];
    private readonly string[] guardNames = ["FLGuard", "FRGuard", "RLHub", "RRHub"];
    private readonly string[] collisionWheelNames = ["FLSurf", "FRSurf", "RLSurf", "RRSurf"];

    private readonly string[] objectPrefixes = ["1", "d", "p", "s", "w"];

    private readonly Dictionary<string, JSObject> actions = [];

    public CGameCtnGhost? CurrentGhost { get; set; }

    private ImmutableList<IInput>? overrideInputs;

    private bool UseHundredths => Map is not null && GameVersionSupport.GetSupportedGameVersion(Map) <= GameVersion.TMF;

    [Parameter, EditorRequired]
    public CGameCtnChallenge? Map { get; set; }

    [Parameter, EditorRequired]
    public View3D? View3D { get; set; }

    [Parameter]
    public EventCallback<CGameCtnChallenge> MapUploaded { get; set; }

    [Parameter]
    public bool MapUploadable { get; set; }

    [Parameter]
    public string? TmxSite { get; set; }

    [Parameter]
    public string? MxSite { get; set; }

    [Parameter]
    public string? ExchangeId { get; set; }

    private async Task OnMapUploadedAsync(CGameCtnChallenge map)
    {
        Map = map;
        await MapUploaded.InvokeAsync(map);
    }

    private string GetPlaybackDescription()
    {
        if (CurrentGhost is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (CurrentGhost.RaceTime is not null)
        {
            sb.Append(CurrentGhost.RaceTime.ToTmString(UseHundredths));
            sb.Append(" by ");
        }

        sb.Append(CurrentGhost.GhostNickname ?? CurrentGhost.GhostLogin);
        return sb.ToString();
    }

    public async ValueTask<bool> TryLoadGhostAsync(CGameCtnGhost ghost, ImmutableList<IInput>? overrideInputs = null)
    {
        var animationModule = await JS.InvokeAsync<IJSObjectReference>("import", $"./js/animation.js");
        await animationModule.InvokeVoidAsync("registerDotNet", DotNetObjectReference.Create(this));

        await JSHost.ImportAsync(nameof(Slide), "../js/slide.js");

        if (View3D is null)
        {
            return false;
        }

        CurrentGhost = ghost;
        this.overrideInputs = overrideInputs;

        if (ghost?.SampleData is null)
        {
            return false;
        }

        ghostSolid = await View3D.LoadGhostAsync(ghost);

        if (ghostSolid is null)
        {
            return false;
        }

        if (ghost.PlayerModel is not null)
        {
            ghostCollisionSolid = await View3D.CreateVehicleCollisionsAsync(ghost.PlayerModel.Id);
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

        if (ghostCollisionSolid is not null)
        {
            ghostCollisionSolid.Position = firstSample.Position;
            ghostCollisionSolid.RotationQuaternion = firstSample.Rotation;
            ghostCollisionSolid.Visible = false;
        }

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
        foreach (var wheel in collisionWheelNames)
        {
            partData[$"{wheel}_steer"] = new double[count];
            partData[$"{wheel}_dampen"] = new double[count];
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

            partData["FLSurf_steer"][i] = s.SteerFront;
            partData["FRSurf_steer"][i] = s.SteerFront;

            partData["FLSurf_dampen"][i] = s.FLDampenLen;
            partData["FRSurf_dampen"][i] = s.FRDampenLen;
            partData["RLSurf_dampen"][i] = s.RLDampenLen;
            partData["RRSurf_dampen"][i] = s.RRDampenLen;
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
        var visualMixer = Animation.CreateMixer(ghostSolid.Object);
        playback?.SetDuration(TimeSpan.FromSeconds(duration));

        actions["Vehicle"] = Animation.CreateAction(visualMixer, Animation.CreateClip("Vehicle", duration, [positionTrack, rotationTrack]));

        foreach (var wheel in wheelNames)
        {
            foreach (var prefix in objectPrefixes)
            {
                var wheelObject = prefix + wheel;

                var node = ghostSolid.GetObjectByName(wheelObject);
                if (node is null) continue;

                Solid.ReorderEuler(node);

                // Rotation
                var rotTrack = Animation.CreateRotationXTrack(times, partData[$"{wheel}_rot"]);
                actions[$"{wheelObject}Rotation"] = Animation.CreateAction(visualMixer,
                    Animation.CreateClip($"{wheelObject}Rotation", duration, [rotTrack]), node);

                // Steer
                var steerTrack = Animation.CreateRotationYTrack(times, partData[$"{wheel}_steer"]);
                actions[$"{wheelObject}Steer"] = Animation.CreateAction(visualMixer,
                    Animation.CreateClip($"{wheelObject}Steer", duration, [steerTrack]), node);

                // Dampen (as relative position)
                var dampenTrack = Animation.CreateRelativePositionYTrack(times, partData[$"{wheel}_dampen"], node);
                actions[$"{wheelObject}Dampen"] = Animation.CreateAction(visualMixer,
                    Animation.CreateClip($"{wheelObject}Dampen", duration, [dampenTrack]), node);

                ghostSolid.Object.GetPropertyAsJSObject("userData")!.SetProperty(wheel, wheelObject);
            }
        }

        foreach (var guard in guardNames)
        {
            foreach (var prefix in objectPrefixes)
            {
                var guardObject = prefix + guard;

                var node = ghostSolid.GetObjectByName(guardObject);
                if (node is null) continue;

                Solid.ReorderEuler(node);

                var steerTrack = Animation.CreateRotationYTrack(times, partData[$"{guard}_steer"]);
                actions[$"{guardObject}Steer"] = Animation.CreateAction(visualMixer,
                    Animation.CreateClip($"{guardObject}Steer", duration, [steerTrack]), node);

                var dampenTrack = Animation.CreateRelativePositionYTrack(times, partData[$"{guard}_dampen"], node);
                actions[$"{guardObject}Dampen"] = Animation.CreateAction(visualMixer,
                    Animation.CreateClip($"{guardObject}Dampen", duration, [dampenTrack]), node);
            }
        }

        if (ghostCollisionSolid is not null)
        {
            var collisionMixer = Animation.CreateMixer(ghostCollisionSolid.Object);

            var collisionPositionTrack = Animation.CreatePositionTrack(times, positions, discrete: true);
            var collisionRotationTrack = Animation.CreateQuaternionTrack(times, rotations, discrete: true);

            actions["VehicleCollision"] = Animation.CreateAction(collisionMixer,
                Animation.CreateClip("VehicleCollision", duration, [collisionPositionTrack, collisionRotationTrack]), ghostCollisionSolid.Object);

            foreach (var wheel in collisionWheelNames)
            {
                var node = ghostCollisionSolid.GetObjectByName(wheel);
                if (node is null) continue;
                Solid.ReorderEuler(node);
                // Steer
                var steerTrack = Animation.CreateRotationYTrack(times, partData[$"{wheel}_steer"], discrete: true);
                actions[$"{wheel}SteerCollision"] = Animation.CreateAction(collisionMixer,
                    Animation.CreateClip($"{wheel}SteerCollision", duration, [steerTrack]), node);
                // Dampen (as relative position)
                var dampenTrack = Animation.CreateRelativePositionYTrack(times, partData[$"{wheel}_dampen"], node, discrete: true);
                actions[$"{wheel}DampenCollision"] = Animation.CreateAction(collisionMixer,
                    Animation.CreateClip($"{wheel}DampenCollision", duration, [dampenTrack]), node);
                ghostCollisionSolid.Object.GetPropertyAsJSObject("userData")!.SetProperty(wheel, wheel);
            }
        }

        var checkpoints = ghost.Checkpoints ?? [];
        var numLaps = ghost.GetNumberOfLaps(Map) ?? 1;
        var perLap = checkpoints.Length / numLaps;
        var respawns = (overrideInputs ?? ghost.Inputs ?? ghost.PlayerInputs?.FirstOrDefault()?.Inputs ?? [])
            .Where(x => x is Respawn { Pressed: true } or RespawnTM2020);

        playback?.SetMarkers(checkpoints.Where(c => c.Time.HasValue).Select((c, i) =>
            new PlaybackMarker
            {
                Time = c.Time!.Value,
                Type = c == checkpoints.Last()
                    ? PlaybackMarkerType.Finish
                    : (((i + 1) % perLap == 0)
                        ? PlaybackMarkerType.Multilap
                        : PlaybackMarkerType.Checkpoint)
            })
                .Concat(respawns.Select(x => new PlaybackMarker
                {
                    Time = x.Time,
                    Type = PlaybackMarkerType.Generic
                }))
            .ToList());

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
            if (checkpoints.Length > 0)
            {
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
                            if (checkpointList is not null)
                            {
                                checkpointList.CurrentCheckpoint = cp.Time;
                                checkpointList.CurrentCheckpointIndex = nextCpTime == double.MaxValue ? (mid - 1) : mid;
                            }
                            prevCheckpointPassedIndex = mid;
                        }

                        if (time < cpTime + 3)
                        {
                            checkpointPassedTime = cp.Time;

                            if (checkpointPassedTime != prevCheckpointPassedTime)
                            {
                                if (checkpoint is not null)
                                {
                                    checkpoint.Time = cp.Time;
                                }
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
                            if (checkpointList is not null)
                            {
                                checkpointList.CurrentCheckpoint = null;
                                checkpointList.CurrentCheckpointIndex = -1;
                            }
                            if (checkpoint is not null)
                            {
                                checkpoint.Time = null;
                            }
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
                    if (checkpoint is not null)
                    {
                        checkpoint.Time = null;
                    }
                    prevCheckpointPassedTime = null;
                }
            }

            var inputs = overrideInputs ?? CurrentGhost.Inputs ?? CurrentGhost.PlayerInputs?.FirstOrDefault()?.Inputs ?? [];
            if (inputs.Count > 0)
            {
                int left = 0, right = inputs.Count - 1, mid = -1;

                // Binary search for the first input within the range
                while (left <= right)
                {
                    mid = (left + right) / 2;
                    var input = inputs[mid];

                    var inputTime = input.Time.TotalSeconds;
                    var nextInputTime = (mid + 1 < inputs.Count)
                        ? inputs[mid + 1].Time.TotalSeconds
                        : double.MaxValue; // Handle last input case

                    if (time >= inputTime && time < nextInputTime)
                    {
                        if (inputList is not null)
                        {
                            inputList.CurrentInput = input.Time;
                            inputList.CurrentInputIndex = nextInputTime == double.MaxValue ? (mid - 1) : mid;
                        }
                        break;
                    }
                    else if (time < inputTime)
                    {
                        right = mid - 1;

                        if (right == -1)
                        {
                            // No input found before this time, reset
                            if (inputList is not null)
                            {
                                inputList.CurrentInput = null;
                                inputList.CurrentInputIndex = -1;
                            }
                        }
                    }
                    else
                    {
                        left = mid + 1;
                    }
                }
            }

            if (CurrentGhost.SampleData?.Samples.Count > 0)
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
                    if (speedometer is not null)
                    {
                        speedometer.RPM = AdditionalMath.Lerp(currentCarSampleToLerp.RPM, nextCarSampleToLerp.RPM, (float)lerpFactor);
                        speedometer.Speed = AdditionalMath.Lerp(currentCarSampleToLerp.VelocitySpeed, nextCarSampleToLerp.VelocitySpeed, (float)lerpFactor);
                    }

                    Drift(currentCarSampleToLerp.FLIsSliding && currentCarSampleToLerp.FLOnGround, "FL");
                    Drift(currentCarSampleToLerp.FRIsSliding && currentCarSampleToLerp.FROnGround, "FR");
                    Drift(currentCarSampleToLerp.RLIsSliding && currentCarSampleToLerp.RLOnGround, "RL");
                    Drift(currentCarSampleToLerp.RRIsSliding && currentCarSampleToLerp.RROnGround, "RR");
                }

                // only per sample update, not interpolated
                if (sampleIndex != prevSampleIndex)
                {
                    //speedometer?.SetSpeed((int)currentSample.VelocitySpeed);
                    ghostInfo?.SetCurrentSample(currentSample);

                    if (currentSample is CSceneVehicleCar.Sample currentCarSample)
                    {
                        //speedometer?.SetGear(currentCarSample.U25 & 7);
                        //speedometer?.SetRPM(carSample.RPM);
                    }

                    prevSampleIndex = sampleIndex;
                }
            }
        }

        prevTime = time;
    }

    private void Drift(bool isSliding, string wheel)
    {
        if (ghostSolid is null || View3D?.Scene is null)
        {
            return;
        }

        var wheelObj = ghostSolid.GetObjectByName(ghostSolid.Object.GetPropertyAsJSObject("userData")!.GetPropertyAsString($"{wheel}Wheel")!);

        if (wheelObj is null)
        {
            return;
        }

        if (isSliding)
        {
            Slide.Do(wheelObj, View3D.Scene.Object);
        }
        else
        {
            Slide.Stop(wheelObj);
        }
    }

    private void OnCheckpointListItemClick(CGameCtnGhost.Checkpoint checkpoint)
    {
        if (checkpoint.Time is null)
        {
            return;
        }

        playback?.Seek(checkpoint.Time.Value, seekPause: false);
    }

    private void OnInputListItemClick(IInput input)
    {
        playback?.Seek(input.Time, seekPause: false);
    }

    public ValueTask DisposeAsync()
    {
        if (RendererInfo.IsInteractive)
        {
            Animation.SetMixerTimeScale(1, isPaused: false);
        }

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

    private void ToggleCollisions(bool collisions)
    {
        if (ghostCollisionSolid is not null)
        {
            ghostCollisionSolid.Visible = collisions;
        }
    }

    private void ChangeCameraType(ReplayCameraType type)
    {
        switch (type)
        {
            case ReplayCameraType.Cam2:
                Camera.RemoveControls();
                break;
            case ReplayCameraType.Orbital:
                View3D?.SetOrbitCamera();
                break;
            case ReplayCameraType.Free:
                Camera.Unfollow();
                View3D?.SetFreeCamera();
                break;
        }
    }

    public void UpdateRenderInfo(RenderDetails details)
    {
        renderInfo?.Update(details);
    }
}
