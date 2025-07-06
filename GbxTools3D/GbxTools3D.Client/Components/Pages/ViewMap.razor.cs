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

    public bool IsDragAndDrop => string.IsNullOrEmpty(TmxSite) && string.IsNullOrEmpty(MxSite);

    public CGameCtnChallenge? Map { get; set; }

    public RenderDetails? RenderDetails { get; set; }

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
        if (!string.IsNullOrEmpty(TmxSite))
        {
            endpoint = $"/api/map/tmx/{TmxSite}/{MapId}";
        }
        else if (!string.IsNullOrEmpty(MxSite))
        {
            endpoint = $"/api/map/mx/{MxSite}/{MapId}";
        }
        else
        {
            throw new Exception();
        }

        using var response = await Http.GetAsync(endpoint);
        var content = await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.MapContentDto);

        if (content is null)
        {
            return;
        }

        await using var ms = new MemoryStream(content.Content);
        Map = Gbx.ParseNode<CGameCtnChallenge>(ms);
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
        RenderDetails = details;
        renderInfo?.Update();
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }
}
