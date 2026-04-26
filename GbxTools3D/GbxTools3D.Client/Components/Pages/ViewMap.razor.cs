using GBX.NET.Engines.Game;
using GBX.NET;
using GbxTools3D.Client.EventArgs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Components.Modules;

namespace GbxTools3D.Client.Components.Pages;

public partial class ViewMap : ComponentBase
{
    private RenderInfo? renderInfo;

    private readonly string[] extensions = ["Challenge.Gbx", "Map.Gbx"];

    [SupplyParameterFromQuery(Name = "tmx")]
    private string? TmxSite { get; set; }

    [SupplyParameterFromQuery(Name = "mx")]
    private string? MxSite { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    private string? MapId { get; set; }

    [SupplyParameterFromQuery(Name = "mapuid")]
    private string? MapUid { get; set; }

    [SupplyParameterFromQuery(Name = "mp")]
    private bool IsManiaPlanetMap { get; set; }

    [SupplyParameterFromQuery(Name = "platform")]
    private string? Platform { get; set; }

    [SupplyParameterFromQuery(Name = "url")]
    private string? Url { get; set; }

    [SupplyParameterFromQuery(Name = "gdrive")]
    private string? GDrive { get; set; }

    [SupplyParameterFromQuery(Name = "gd")]
    private string? Gd { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(TmxSite) && string.IsNullOrEmpty(MxSite) && string.IsNullOrEmpty(Url) && string.IsNullOrEmpty(MapUid) && string.IsNullOrEmpty(GDrive) && string.IsNullOrEmpty(Gd);

    public CGameCtnChallenge? Map { get; set; }

    private string selectedExternal = "tmx";
    private string selectedTmx = "tmnf";
    private string selectedMx = "tm2020";
    private string externalId = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (IsDragAndDrop)
        {
            Map = GbxService.SelectedMap?.Node;
            return;
        }

        string endpoint;
        if (!string.IsNullOrEmpty(Url))
        {
            endpoint = Url;
        }
        else if (IsManiaPlanetMap)
        {
            endpoint = $"/api/map/mp/{MapUid}";
        }
        else if (!string.IsNullOrEmpty(Platform))
        {
            endpoint = $"/api/map/tmt/{Platform}/uid/{MapUid}";
        }
        else if (!string.IsNullOrEmpty(TmxSite))
        {
            endpoint = $"/api/map/tmx/{TmxSite}/id/{MapId}";
        }
        else if (!string.IsNullOrEmpty(MxSite))
        {
            endpoint = $"/api/map/mx/{MxSite}/id/{MapId}";
        }
        else if (GDrive is not null || Gd is not null)
        {
            endpoint = $"/api/map/gdrive/{GDrive ?? Gd}";
        }
        else
        {
            throw new Exception("This should not happen");
        }

        using var response = await Http.GetAsync(endpoint);

        if (!string.IsNullOrEmpty(Url) || !string.IsNullOrEmpty(MapUid) || !string.IsNullOrEmpty(GDrive) || !string.IsNullOrEmpty(Gd))
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            Map = await Gbx.ParseAsync<CGameCtnChallenge>(stream);
        }
        else
        {
            var content = await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.MapContentDto);

            if (content is null)
            {
                return;
            }

            await using var ms = new MemoryStream(content.Content);
            Map = Gbx.ParseNode<CGameCtnChallenge>(ms);
        }
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        if (gbx is Gbx<CGameCtnChallenge> map)
        {
            GbxService.Add(map);
            Map = map.Node;
        }
    }

    private void OnRenderDetails(RenderDetails details)
    {
        renderInfo?.Update(details);
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }
}
