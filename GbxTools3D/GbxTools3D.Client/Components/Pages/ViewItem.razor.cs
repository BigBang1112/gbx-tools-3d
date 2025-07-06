using GBX.NET;
using GBX.NET.Engines.GameData;
using GbxTools3D.Client.EventArgs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace GbxTools3D.Client.Components.Pages;

public partial class ViewItem : ComponentBase
{
    private readonly string[] extensions = ["Item.Gbx", "Block.Gbx"];

    [SupplyParameterFromQuery(Name = "type")]
    private string? Type { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    private string? ItemId { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(Type);

    private CGameItemModel? item;

    private string selectedExternal = "ix";
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
            item = GbxService.SelectedItem?.Node;
            return;
        }

        string endpoint;
        if (!string.IsNullOrEmpty(Type))
        {
            endpoint = $"/api/item/{Type}/{ItemId}";
        }
        else
        {
            throw new Exception();
        }

        using var response = await Http.GetAsync(endpoint);
        var content = await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ItemContentDto);

        if (content is null)
        {
            return;
        }

        await using var ms = new MemoryStream(content.Content);
        item = Gbx.ParseNode<CGameItemModel>(ms);
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        if (gbx is Gbx<CGameItemModel> item)
        {
            GbxService.Add(item);
            this.item = item.Node;
        }
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }
}
