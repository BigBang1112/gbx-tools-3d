using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Enums;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GbxTools3D.Client.Components.Pages;

public partial class Catalog
{
    private View3D? view3d;

    private Virtualize<BlockInfoDto>? blocksVirtualize;
    private Virtualize<VehicleDto>? vehiclesVirtualize;
    private IJSObjectReference? module;

    private bool showCatalog = true;

    [Parameter]
    public string? GameVersion { get; set; }

    [Parameter]
    public string? ContentType { get; set; }

    [Parameter]
    public string? CollectionName { get; set; }

    [Parameter]
    public string? AssetType { get; set; }

    [SupplyParameterFromQuery(Name = "selected")]
    private string? AssetName { get; set; }

    [SupplyParameterFromQuery(Name = "nocatalog")]
    private bool NoCatalog { get; set; }

    public GameVersion GameVersionEnum { get; set; }
    public ContentType ContentTypeEnum { get; set; }
    public AssetType AssetTypeEnum { get; set; }

    public CollectionDto? SelectedCollection => ContentTypeEnum == Enums.ContentType.Collection
        ? CollectionClientService.Collections?.FirstOrDefault(c => c.Name == CollectionName) : null;

    private string assetSearchValue = "";

    public string AssetSearchValue
    {
        get => assetSearchValue;
        set
        {
            if (assetSearchValue == value)
            {
                return;
            }

            assetSearchValue = value;

            blocksVirtualize?.RefreshDataAsync();
            vehiclesVirtualize?.RefreshDataAsync();
        }
    }

    private void OnAssetSearchInput(ChangeEventArgs e)
    {
        AssetSearchValue = e.Value?.ToString() ?? "";
    }

    private ValueTask<ItemsProviderResult<T>> FilterAssets<T>(List<T> allAssets, Func<T, string> filterBy, ItemsProviderRequest request)
    {
        var filteredAssets = allAssets.Where(b => filterBy(b).Contains(AssetSearchValue, StringComparison.OrdinalIgnoreCase));

        var totalCount = filteredAssets.Count();
        var assets = filteredAssets
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        return ValueTask.FromResult(new ItemsProviderResult<T>(assets, totalCount));
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Components/Pages/Catalog.razor.js");
            //await module.InvokeVoidAsync("addHandlers", assets);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Enum.TryParse(GameVersion, true, out GameVersion gameVersion))
        {
            GameVersionEnum = gameVersion;
        }
        else
        {
            GameVersionEnum = GBX.NET.GameVersion.Unspecified;
        }

        ContentTypeEnum = ContentType?.ToLowerInvariant() switch
        {
            "collections" => Enums.ContentType.Collection,
            "vehicles" => Enums.ContentType.Vehicle,
            _ => Enums.ContentType.None
        };

        AssetTypeEnum = AssetType?.ToLowerInvariant() switch
        {
            "blocks" => Enums.AssetType.Block,
            "decorations" => Enums.AssetType.Decoration,
            "items" => Enums.AssetType.Item,
            "macroblocks" => Enums.AssetType.Macroblock,
            _ => Enums.AssetType.Block
        };

        await FetchInfoAsync();
    }

    private async Task FetchInfoAsync(CancellationToken cancellationToken = default)
    {
        if (GameVersionEnum == GBX.NET.GameVersion.Unspecified)
        {
            return;
        }

        await CollectionClientService.FetchAllAsync(GameVersionEnum, cancellationToken);

        if (AssetTypeEnum == Enums.AssetType.None)
        {
            return;
        }

        if (ContentTypeEnum == Enums.ContentType.Collection && CollectionName is not null)
        {
            switch (AssetTypeEnum)
            {
                case Enums.AssetType.Block:
                    await BlockClientService.FetchAllAsync(GameVersionEnum, CollectionName, cancellationToken);
                    if (blocksVirtualize is not null)
                    {
                        await blocksVirtualize.RefreshDataAsync();
                    }
                    break;
                case Enums.AssetType.Decoration:
                    await DecorationClientService.FetchAllAsync(GameVersionEnum, CollectionName, cancellationToken);
                    break;
            }
        }

        if (ContentTypeEnum == Enums.ContentType.Vehicle)
        {
            await VehicleClientService.FetchAllAsync(GameVersionEnum, cancellationToken);
            if (vehiclesVirtualize is not null)
            {
                await vehiclesVirtualize.RefreshDataAsync();
            }
        }
    }

    private string? GetAssetTypeString()
    {
        return AssetTypeEnum switch
        {
            Enums.AssetType.Block => "blocks",
            Enums.AssetType.Decoration => "decorations",
            Enums.AssetType.Item => "items",
            Enums.AssetType.Macroblock => "macroblocks",
            _ => null
        };
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
