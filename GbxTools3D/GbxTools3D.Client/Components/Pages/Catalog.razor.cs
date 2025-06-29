using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Enums;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Client.Modules;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace GbxTools3D.Client.Components.Pages;

[SupportedOSPlatform("browser")]
public partial class Catalog : ComponentBase
{
    private View3D? view3d;

    private Virtualize<BlockInfoDto>? blocksVirtualize;
    private Virtualize<VehicleDto>? vehiclesVirtualize;
    private IJSObjectReference? module;

    private bool showCatalog = true;
    private bool showProperties = true;

    private BlockInfoDto? block;
    private bool blockIsGround;
    private int blockVariant;
    private int blockSubVariant;

    private DecorationSizeDto? decoSize;

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

    [SupplyParameterFromQuery(Name = "scene")]
    private string? SceneName { get; set; }

    [SupplyParameterFromQuery(Name = "nocatalog")]
    private bool NoCatalog { get; set; }

    public GameVersion GameVersionEnum { get; set; }
    public ContentType ContentTypeEnum { get; set; }
    public AssetType AssetTypeEnum { get; set; }

    public CollectionDto? SelectedCollection => ContentTypeEnum == Enums.ContentType.Collection
        ? CollectionClientService.Collections?.FirstOrDefault(c => c.Name == CollectionName) : null;

    private string? materialName;
    private string? shaderName;
    private JSObject? selectedTreeObject;

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
    
        if (view3d is not null)
        {
            // the view3d is available after the render event, however, to guarantee only a single subscription to an event, gotta first unsubscribe
            view3d.OnIntersect -= OnFocusedSolidHover;
            view3d.OnIntersect += OnFocusedSolidHover;
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
        block = null;

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
                    var allBlocksTask = BlockClientService.FetchAllAsync(GameVersionEnum, CollectionName, cancellationToken);
                    var blockTask = AssetName is null || !RendererInfo.IsInteractive ? null : BlockClientService.GetAsync(GameVersionEnum, CollectionName, AssetName, cancellationToken);

                    await allBlocksTask;
                    if (blocksVirtualize is not null)
                    {
                        await blocksVirtualize.RefreshDataAsync();
                    }

                    block = BlockClientService.Blocks.FirstOrDefault(x => x.Name == AssetName);
                    blockIsGround = block?.AirVariants.Count == 0;
                    blockVariant = blockIsGround
                        ? block?.GroundVariants.FirstOrDefault()?.Variant ?? 0
                        : block?.AirVariants.FirstOrDefault()?.Variant ?? 0;
                    blockSubVariant = 0;

                    if (blockTask is not null)
                    {
                        block = await blockTask;
                    }
                    break;
                case Enums.AssetType.Decoration:
                    await DecorationClientService.FetchAllAsync(GameVersionEnum, CollectionName, cancellationToken);

                    var decoSizeArray = AssetName is null ? [] : AssetName.Split('x');

                    if (decoSizeArray.Length == 3 && int.TryParse(decoSizeArray[0], out var decoSizeX)
                        && int.TryParse(decoSizeArray[1], out var decoSizeY)
                        && int.TryParse(decoSizeArray[2], out var decoSizeZ))
                    {
                        var size = new Int3(decoSizeX, decoSizeY, decoSizeZ);

                        var decoSizes = DecorationClientService.DecorationSizes
                            .Where(x => x.Size == size)
                            .ToLookup(x => x.Size);
                        
                        if (decoSizes.Count > 1)
                        {
                            decoSize = decoSizes[size].FirstOrDefault(x => x.SceneName == SceneName)
                                ?? decoSizes[size].FirstOrDefault();
                        }
                        else if (decoSizes.Count == 1)
                        {
                            decoSize = decoSizes[size].FirstOrDefault();
                        }
                        else
                        {
                            decoSize = null;
                        }
                    }
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

    private void OnFocusedSolidHover(IntersectionInfo intersection)
    {
        materialName = intersection.ObjectName;

        if (intersection.MaterialUserData?.RootElement.TryGetProperty("shaderName", out var shaderJson) == true)
        {
            shaderName = shaderJson.GetString();
        }

        StateHasChanged();
    }

    internal string? TestGetSoundHash(BlockInfoDto blockInfo)
    {
        var firstSoundPath = blockInfo.AirVariants
            .Concat(blockInfo.GroundVariants)
            .FirstOrDefault(x => x.ObjectLinks?.Any(x => x.SoundPath is not null) ?? false)
            ?.ObjectLinks?.FirstOrDefault(x => x.SoundPath is not null)?.SoundPath;

        if (firstSoundPath is null)
        {
            return null;
        }

        return $"GbxTools3D|Sound|{GameVersionEnum}|{firstSoundPath}|ItsChallengeNotAltered".Hash();
    }

    private async Task ChangeBlockVariantAsync(bool isGround)
    {
        if (block is null || blockIsGround == isGround)
        {
            return;
        }

        blockIsGround = isGround;

        var variant = blockIsGround
            ? block.GroundVariants.First(x => x.Variant == blockVariant && x.SubVariant == blockSubVariant)
            : block.AirVariants.First(x => x.Variant == blockVariant && x.SubVariant == blockSubVariant);

        if (view3d is not null)
        {
            await view3d.ChangeBlockVariantAsync(blockIsGround, variant.Variant, variant.SubVariant);
        }
    }

    private async Task ChangeBlockVariantAsync(BlockVariantDto variant)
    {
        blockVariant = variant.Variant;
        blockSubVariant = variant.SubVariant;

        if (view3d is not null)
        {
            await view3d.ChangeBlockVariantAsync(blockIsGround, blockVariant, blockSubVariant);
        }
    }

    private void OnSelectedTreeObjectChanged(JSObject treeObject)
    {
        selectedTreeObject = treeObject;

        view3d?.ResetLightHelper();

        foreach (var child in Solid.GetChildren(treeObject))
        {
            if (child.GetPropertyAsBoolean("isSpotLight"))
            {
                view3d?.SetLightHelper(child, Solid.CreateSpotLightHelper);
            }
            else if (child.GetPropertyAsBoolean("isLight"))
            {
                view3d?.SetLightHelper(child, Solid.CreatePointLightHelper);
            }
        }
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

        if (view3d is not null)
        {
            view3d.OnIntersect -= OnFocusedSolidHover;
        }
    }
}
