using GbxTools3D.Client.Models;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Runtime.Versioning;
using TmEssentials;

namespace GbxTools3D.Client.Components;

[SupportedOSPlatform("browser")]
public partial class Playback : ComponentBase
{
    private ElementReference progress;

    private IJSObjectReference? module;
    private DotNetObjectReference<Playback>? objRef;

    [Parameter, EditorRequired]
    public EventCallback<bool> OnPlay { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnPause { get; set; }

    [Parameter, EditorRequired]
    public EventCallback OnRewind { get; set; }

    [Parameter]
    public List<PlaybackMarker> Markers { get; set; } = [];

    public bool IsPlaying { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsSeekPaused { get; private set; }

    public TimeSpan CurrentTime { get; private set; }
    public TimeSpan Duration { get; set; }

    public TimeSpan? PreviewTime { get; set; }

    protected override void OnInitialized() =>
        objRef = DotNetObjectReference.Create(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Components/Playback.razor.js");
            await module.InvokeVoidAsync("addHandlers", progress, objRef);
        }
    }

    private async Task PlayAsync()
    {
        var firstPlay = false;

        if (!IsPlaying)
        {
            IsPlaying = true;
            firstPlay = true;
        }
        else
        {
            IsPaused = !IsPaused;
        }

        if (IsPaused)
        {
            await OnPause.InvokeAsync();
        }
        else
        {
            await OnPlay.InvokeAsync(firstPlay);
        }
    }

    private async Task PauseAsync()
    {
        IsPaused = true;
        await OnPause.InvokeAsync();
    }

    private async Task RewindAsync()
    {
        CurrentTime = TimeSpan.Zero;
        await OnRewind.InvokeAsync();
    }

    public void SetTime(TimeSpan time)
    {
        CurrentTime = time;
        StateHasChanged();
    }

    public void SetDuration(TimeInt32 time)
    {
        Duration = time;
        StateHasChanged();
    }

    public async ValueTask<bool> IsSeekingAsync()
    {
        return module is not null && await module.InvokeAsync<bool>("isSeeking");
    }

    [JSInvokable]
    public async Task Seek(double percentage)
    {
        if (!IsPlaying)
        {
            await PlayAsync();
            await PauseAsync();
        }

        if (!IsPaused)
        {
            IsSeekPaused = true;
            await PauseAsync();
        }

        var newTime = Duration.TotalSeconds * percentage;
        SetTime(TimeSpan.FromSeconds(newTime));
        Animation.SetMixerTime(newTime);
    }

    [JSInvokable]
    public async Task EndSeekAsync()
    {
        if (IsSeekPaused)
        {
            IsSeekPaused = false;
            await PlayAsync();
        }
    }

    [JSInvokable]
    public void ShowPreviewTime(double percentage)
    {
        PreviewTime = TimeSpan.FromSeconds(Duration.TotalSeconds * percentage);
        StateHasChanged();
    }

    public void SetMarkers(List<PlaybackMarker> markers)
    {
        Markers = markers;
        StateHasChanged();
    }

    private async Task OnMarkerMouseDown(PlaybackMarker marker)
    {
        var newTime = marker.Time.TotalSeconds;
        SetTime(TimeSpan.FromSeconds(newTime));
        Animation.SetMixerTime(newTime);
        if (module is not null)
        {
            await module.InvokeVoidAsync("stopSeeking");
        }
        await InvokeAsync(StateHasChanged);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (module is not null)
        {
            try
            {
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
