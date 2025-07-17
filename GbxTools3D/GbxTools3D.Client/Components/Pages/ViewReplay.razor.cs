using GBX.NET;
using GBX.NET.Engines.Game;
using GbxTools3D.Client.Components.Modules;
using GbxTools3D.Client.EventArgs;
using GbxTools3D.Client.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Components.Pages;

[SupportedOSPlatform("browser")]
public partial class ViewReplay : ComponentBase, IAsyncDisposable
{
    private View3D? view3d;
    private GhostControls? ghostControls;

    private readonly string[] extensions = ["Replay.Gbx"];

    [SupplyParameterFromQuery(Name = "tmx")]
    private string? TmxSite { get; set; }

    [SupplyParameterFromQuery(Name = "mx")]
    private string? MxSite { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    private string? ReplayId { get; set; }

    [SupplyParameterFromQuery(Name = "mapid")]
    private string? MapId { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(TmxSite) && string.IsNullOrEmpty(MxSite);

    public CGameCtnReplayRecord? Replay { get; set; }
    public CGameCtnGhost? CurrentGhost { get; set; }

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
            endpoint = $"/api/replay/tmx/{TmxSite}/{ReplayId}";
        }
        else if (!string.IsNullOrEmpty(MxSite))
        {
            endpoint = $"/api/replay/mx/{MxSite}/{ReplayId}";
        }
        else
        {
            throw new Exception();
        }

        if (string.IsNullOrEmpty(MapId))
        {
            endpoint += $"/{MapId}";
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

        if (ghostControls is not null)
        {
            await ghostControls.TryLoadGhostAsync(ghost, Replay.Inputs);
        }

        return true;
    }

    private void OnRenderDetails(RenderDetails details)
    {
        ghostControls?.UpdateRenderInfo(details);
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible
        return ValueTask.CompletedTask;
    }
}
