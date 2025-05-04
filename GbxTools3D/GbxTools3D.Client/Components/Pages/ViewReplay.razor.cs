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
using System.Xml;

namespace GbxTools3D.Client.Components.Pages;

[SupportedOSPlatform("browser")]
public partial class ViewReplay : ComponentBase
{
    private View3D? view3d;
    private Playback? playback;

    private JSObject? action;
    private JSObject? actionFLWheelRotation;
    private JSObject? actionFLWheelSteer;
    private JSObject? actionFRWheelRotation;
    private JSObject? actionFRWheelSteer;
    private JSObject? actionRLWheelRotation;
    private JSObject? actionRRWheelRotation;
    private JSObject? actionFLGuardSteer;
    private JSObject? actionFRGuardSteer;
    private JSObject? actionFLDampen;
    private JSObject? actionFRDampen;
    private JSObject? actionRLDampen;
    private JSObject? actionRRDampen;

    private readonly string[] extensions = ["Replay.Gbx"];

    [SupplyParameterFromQuery(Name = "tmx")]
    private string? TmxSite { get; set; }

    [SupplyParameterFromQuery(Name = "mx")]
    private string? MxSite { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    private string? MapId { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(TmxSite) && string.IsNullOrEmpty(MxSite);

    public CGameCtnReplayRecord? Replay { get; set; }

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

        if (ghost is null)
        {
            return false;
        }

        if (ghost.SampleData is null)
        {
            return false;
        }

        var ghostSolid = await view3d.LoadGhostAsync(ghost);

        if (ghostSolid is null)
        {
            return false;
        }

        var samples = ghost.SampleData.Samples;

        var firstSample = samples.FirstOrDefault();

        if (firstSample is null)
        {
            return false;
        }

        var lastSample = samples.Last();

        ghostSolid.Position = firstSample.Position;
        ghostSolid.RotationQuaternion = firstSample.Rotation;

        var times = new double[samples.Count];
        var positions = new double[samples.Count * 3];
        var rotations = new double[samples.Count * 4];
        var fLWheelRotation = new double[samples.Count];
        var fRWheelRotation = new double[samples.Count];
        var rLWheelRotation = new double[samples.Count];
        var rRWheelRotation = new double[samples.Count];
        var fLWheelSteer = new double[samples.Count];
        var fRWheelSteer = new double[samples.Count];
        var fLGuardSteer = new double[samples.Count];
        var fRGuardSteer = new double[samples.Count];
        var fLDampen = new double[samples.Count];
        var fRDampen = new double[samples.Count];
        var rLDampen = new double[samples.Count];
        var rRDampen = new double[samples.Count];

        for (var i = 0; i < samples.Count; i++)
        {
            var sample = (CSceneVehicleCar.Sample)samples[i];
            times[i] = sample.Time.TotalSeconds;
            positions[i * 3] = sample.Position.X;
            positions[i * 3 + 1] = sample.Position.Y;
            positions[i * 3 + 2] = sample.Position.Z;
            rotations[i * 4] = sample.Rotation.X;
            rotations[i * 4 + 1] = sample.Rotation.Y;
            rotations[i * 4 + 2] = sample.Rotation.Z;
            rotations[i * 4 + 3] = sample.Rotation.W;
            fLWheelRotation[i] = sample.FLWheelRotation;
            fLWheelSteer[i] = sample.SteerFront;
            fRWheelRotation[i] = sample.FRWheelRotation;
            fRWheelSteer[i] = sample.SteerFront;
            rLWheelRotation[i] = sample.RLWheelRotation;
            rRWheelRotation[i] = sample.RRWheelRotation;
            fLGuardSteer[i] = sample.SteerFront;
            fRGuardSteer[i] = sample.SteerFront;
            fLDampen[i] = sample.FLDampenLen;
            fRDampen[i] = sample.FRDampenLen;
            rLDampen[i] = sample.RLDampenLen;
            rRDampen[i] = sample.RRDampenLen;
        }

        var positionTrack = Animation.CreatePositionTrack(times, positions);
        var rotationTrack = Animation.CreateQuaternionTrack(times, rotations);
        var fLWheelRotationTrack = Animation.CreateRotationXTrack(times, fLWheelRotation);
        var fLWheelSteerTrack = Animation.CreateRotationYTrack(times, fLWheelSteer);
        var fRWheelRotationTrack = Animation.CreateRotationXTrack(times, fRWheelRotation);
        var fRWheelSteerTrack = Animation.CreateRotationYTrack(times, fRWheelSteer);
        var rLWheelRotationTrack = Animation.CreateRotationXTrack(times, rLWheelRotation);
        var rRWheelRotationTrack = Animation.CreateRotationXTrack(times, rRWheelRotation);
        var fLGuardSteerTrack = Animation.CreateRotationYTrack(times, fLGuardSteer);
        var fRGuardSteerTrack = Animation.CreateRotationYTrack(times, fRGuardSteer);

        var fLWheel = ghostSolid.GetObjectByName("1FLWheel");
        var fRWheel = ghostSolid.GetObjectByName("1FRWheel");
        var rLWheel = ghostSolid.GetObjectByName("1RLWheel");
        var rRWheel = ghostSolid.GetObjectByName("1RRWheel");
        var fLGuard = ghostSolid.GetObjectByName("1FLGuard");
        var fRGuard = ghostSolid.GetObjectByName("1FRGuard");

        var fLDampenTrack = Animation.CreateRelativePositionYTrack(times, fLDampen, fLWheel);
        var fRDampenTrack = Animation.CreateRelativePositionYTrack(times, fRDampen, fRWheel);
        var rLDampenTrack = Animation.CreateRelativePositionYTrack(times, rLDampen, rLWheel);
        var rRDampenTrack = Animation.CreateRelativePositionYTrack(times, rRDampen, rRWheel);

        var clipVehicle = Animation.CreateClip("Vehicle", times.Last(), [positionTrack, rotationTrack]);
        var clipFLWheelRotation = Animation.CreateClip("FLWheelRotation", times.Last(), [fLWheelRotationTrack]);
        var clipFLWheelSteer = Animation.CreateClip("FLWheelSteer", times.Last(), [fLWheelSteerTrack]);
        var clipFRWheelRotation = Animation.CreateClip("FRWheelRotation", times.Last(), [fRWheelRotationTrack]);
        var clipFRWheelSteer = Animation.CreateClip("FRWheelSteer", times.Last(), [fRWheelSteerTrack]);
        var clipRLWheelRotation = Animation.CreateClip("RLWheelRotation", times.Last(), [rLWheelRotationTrack]);
        var clipRRWheelRotation = Animation.CreateClip("RRWheelRotation", times.Last(), [rRWheelRotationTrack]);
        var clipFLDampen = Animation.CreateClip("FLDampen", times.Last(), [fLDampenTrack]);
        var clipFRDampen = Animation.CreateClip("FRDampen", times.Last(), [fRDampenTrack]);
        var clipRLDampen = Animation.CreateClip("RLDampen", times.Last(), [rLDampenTrack]);
        var clipRRDampen = Animation.CreateClip("RRDampen", times.Last(), [rRDampenTrack]);

        Animation.CreateMixer(ghostSolid.Object);
        action = Animation.CreateAction(clipVehicle);
        actionFLWheelRotation = Animation.CreateAction(clipFLWheelRotation, fLWheel);
        actionFLWheelSteer = Animation.CreateAction(clipFLWheelSteer, fLWheel);
        actionFRWheelRotation = Animation.CreateAction(clipFRWheelRotation, fRWheel);
        actionFRWheelSteer = Animation.CreateAction(clipFRWheelSteer, fRWheel);
        actionRLWheelRotation = Animation.CreateAction(clipRLWheelRotation, rLWheel);
        actionRRWheelRotation = Animation.CreateAction(clipRRWheelRotation, rRWheel);
        actionFLDampen = Animation.CreateAction(clipFLDampen, fLWheel);
        actionFRDampen = Animation.CreateAction(clipFRDampen, fRWheel);
        actionRLDampen = Animation.CreateAction(clipRLDampen, rLWheel);
        actionRRDampen = Animation.CreateAction(clipRRDampen, rRWheel);

        Solid.ReorderEuler(fLWheel);
        Solid.ReorderEuler(fRWheel);
        Solid.ReorderEuler(rLWheel);
        Solid.ReorderEuler(rRWheel);

        if (fLGuard is not null)
        {
            Solid.ReorderEuler(fLGuard);
            var clipFLGuardSteer = Animation.CreateClip("FLGuardSteer", times.Last(), [fLGuardSteerTrack]);
            actionFLGuardSteer = Animation.CreateAction(clipFLGuardSteer, fLGuard);
        }

        if (fRGuard is not null)
        {
            Solid.ReorderEuler(fRGuard);
            var clipFRGuardSteer = Animation.CreateClip("FRGuardSteer", times.Last(), [fRGuardSteerTrack]);
            actionFRGuardSteer = Animation.CreateAction(clipFRGuardSteer, fRGuard);
        }

        var checkpoints = ghost.Checkpoints ?? [];
        var numLaps = GetNumberOfLaps(ghost.Validate_RaceSettings);
        var checkpointsPerLap = checkpoints.Length / numLaps;

        playback?.SetDuration(lastSample.Time);
        playback?.SetMarkers(checkpoints.Where(x => x.Time.HasValue).Select((c, i) => new PlaybackMarker
        {
            Time = c.Time.GetValueOrDefault(),
            Type = c == checkpoints.Last() ? PlaybackMarkerType.Finish : (((i + 1) % checkpointsPerLap == 0) ? PlaybackMarkerType.Multilap : PlaybackMarkerType.Checkpoint),
        }).ToList() ?? []);

        return true;
    }

    [JSInvokable]
    public void UpdateTimeline(double timeIncludingRepeats)
    {
        var time = action?.GetPropertyAsDouble("time") ?? 0;
        playback?.SetTime(TimeSpan.FromSeconds(time));
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }

    private void Play(bool firstPlay)
    {
        if (action is null
            || actionFLWheelRotation is null
            || actionFLWheelSteer is null
            || actionFRWheelRotation is null
            || actionFRWheelSteer is null
            || actionRLWheelRotation is null
            || actionRRWheelRotation is null
            || actionFLDampen is null
            || actionFRDampen is null
            || actionRLDampen is null
            || actionRRDampen is null)
        {
            return;
        }

        if (firstPlay)
        {
            Animation.PlayAction(action);
            Animation.PlayAction(actionFLWheelRotation);
            Animation.PlayAction(actionFLWheelSteer);
            Animation.PlayAction(actionFRWheelRotation);
            Animation.PlayAction(actionFRWheelSteer);
            Animation.PlayAction(actionRLWheelRotation);
            Animation.PlayAction(actionRRWheelRotation);
            if (actionFLGuardSteer is not null) Animation.PlayAction(actionFLGuardSteer);
            if (actionFRGuardSteer is not null) Animation.PlayAction(actionFRGuardSteer);
            Animation.PlayAction(actionFLDampen);
            Animation.PlayAction(actionFRDampen);
            Animation.PlayAction(actionRLDampen);
            Animation.PlayAction(actionRRDampen);
        }

        Animation.PlayMixer();

        /*if (timelinePlayer.IsPaused)
        {
            Animation.PauseAction(action);
            Animation.PauseAction(actionFLWheelRotation);
            Animation.PauseAction(actionFLWheelSteer);
            Animation.PauseAction(actionFRWheelRotation);
            Animation.PauseAction(actionFRWheelSteer);
            Animation.PauseAction(actionRLWheelRotation);
            Animation.PauseAction(actionRRWheelRotation);
            if (actionFLGuardSteer is not null) Animation.PauseAction(actionFLGuardSteer);
            if (actionFRGuardSteer is not null) Animation.PauseAction(actionFRGuardSteer);
            Animation.PauseAction(actionFLDampen);
            Animation.PauseAction(actionFRDampen);
            Animation.PauseAction(actionRLDampen);
            Animation.PauseAction(actionRRDampen);
        }
        else
        {
            Animation.ResumeAction(action);
            Animation.ResumeAction(actionFLWheelRotation);
            Animation.ResumeAction(actionFLWheelSteer);
            Animation.ResumeAction(actionFRWheelRotation);
            Animation.ResumeAction(actionFRWheelSteer);
            Animation.ResumeAction(actionRLWheelRotation);
            Animation.ResumeAction(actionRRWheelRotation);
            if (actionFLGuardSteer is not null) Animation.ResumeAction(actionFLGuardSteer);
            if (actionFRGuardSteer is not null) Animation.ResumeAction(actionFRGuardSteer);
            Animation.ResumeAction(actionFLDampen);
            Animation.ResumeAction(actionFRDampen);
            Animation.ResumeAction(actionRLDampen);
            Animation.ResumeAction(actionRRDampen);
        }*/
    }

    private void Pause()
    {
        Animation.PauseMixer();
    }

    private void Rewind()
    {
        Animation.SetMixerTime(0);
    }

    private int GetNumberOfLaps(string? raceSettingsXml)
    {
        if (raceSettingsXml is null || raceSettingsXml == "1P-Time")
        {
            return GetNbLapsFromMap();
        }

        // TODO: use MiniXmlReader
        var readerSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };

        using var strReader = new StringReader(raceSettingsXml);
        using var reader = XmlReader.Create(strReader, readerSettings);

        try
        {
            reader.ReadToDescendant("laps");

            return reader.ReadElementContentAsInt();
        }
        catch
        {
            return GetNbLapsFromMap();
        }
    }

    private int GetNbLapsFromMap()
    {
        var map = Replay?.Challenge;
        return map?.IsLapRace == true ? map.NbLaps  : 1;
    }
}
