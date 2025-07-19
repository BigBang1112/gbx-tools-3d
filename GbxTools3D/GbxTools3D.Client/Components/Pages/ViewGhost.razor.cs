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
public partial class ViewGhost
{
    private View3D? view3d;
    private GhostControls? ghostControls;

    private readonly string[] extensions = ["Ghost.Gbx"];

    [SupplyParameterFromQuery(Name = "type")]
    private string? Type { get; set; }

    [SupplyParameterFromQuery(Name = "mx")]
    private string? MxSite { get; set; }

    [SupplyParameterFromQuery(Name = "mapuid")]
    private string? MapUid { get; set; }

    [SupplyParameterFromQuery(Name = "time")]
    private int? Time { get; set; }

    [SupplyParameterFromQuery(Name = "login")]
    private string? Login { get; set; }

    [SupplyParameterFromQuery(Name = "url")]
    private string? Url { get; set; }

    [SupplyParameterFromQuery(Name = "mapurl")]
    private string? MapUrl { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(Type) && string.IsNullOrEmpty(Url);

    private string selectedExternal = "tmio";

    public CGameCtnGhost? Ghost { get; set; }
    public CGameCtnChallenge? Map { get; set; }

    private CGameCtnChallenge? mapAfterGhost;

    private readonly SemaphoreSlim semaphore = new(1, 1);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (IsDragAndDrop)
        {
            Ghost = GbxService.SelectedGhost?.Node;
            return;
        }

        await semaphore.WaitAsync();

        var ghostResponseTask = default(Task<HttpResponseMessage>);
        var mapResponseTask = default(Task<HttpResponseMessage>);

        try
        {
            if (!string.IsNullOrEmpty(Url))
            {
                ghostResponseTask = Http.GetAsync($"/api/ghost/wrr/{MapUid}/{Time}/{Login}");
            }
            else if (Type == "wrr")
            {
                if (MapUid is not null && Time.HasValue && Login is not null)
                {
                    ghostResponseTask = Http.GetAsync($"/api/ghost/wrr/{MapUid}/{Time}/{Login}");
                }
            }

            if (!string.IsNullOrEmpty(MapUrl))
            {
                mapResponseTask = Http.GetAsync(MapUrl);
            }
            else if (MxSite is not null && MapUid is not null)
            {
                mapResponseTask = Http.GetAsync($"/api/map/mx/{MxSite}/uid/{MapUid}");
            }

            if (ghostResponseTask is not null)
            {
                using var response = await ghostResponseTask;
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    Ghost = await Gbx.ParseAsync<CGameCtnGhost>(stream);
                }
            }

            if (mapResponseTask is not null)
            {
                using var response = await mapResponseTask;
                if (response.IsSuccessStatusCode)
                {
                    if (string.IsNullOrEmpty(MapUrl))
                    {
                        var content = await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.MapContentDto);

                        if (content is not null)
                        {
                            await using var ms = new MemoryStream(content.Content);
                            mapAfterGhost = Gbx.ParseNode<CGameCtnChallenge>(ms);
                        }
                    }
                    else
                    {
                        await using var stream = await response.Content.ReadAsStreamAsync();
                        mapAfterGhost = await Gbx.ParseAsync<CGameCtnChallenge>(stream);
                    }
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        if (gbx is Gbx<CGameCtnGhost> ghost)
        {
            GbxService.Add(ghost);
            Ghost = ghost.Node;
        }
    }

    private async Task AfterSceneLoadAsync()
    {
        await semaphore.WaitAsync();

        try
        {
            await TryLoadGhostAsync();

            if (string.IsNullOrEmpty(MapUid) && string.IsNullOrEmpty(MapUrl))
            {
                return;
            }

            Map = mapAfterGhost;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async ValueTask<bool> TryLoadGhostAsync()
    {
        if (view3d is null || Ghost is null)
        {
            return false;
        }

        if (ghostControls is not null)
        {
            await ghostControls.TryLoadGhostAsync(Ghost);
        }

        return true;
    }

    private void OnRenderDetails(RenderDetails details)
    {
        ghostControls?.UpdateRenderInfo(details);
    }

    private async Task OnMapUploadedAsync(CGameCtnChallenge map)
    {
        Map = map;

        if (view3d is not null)
        {
            await view3d.TryLoadMapAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }
}
