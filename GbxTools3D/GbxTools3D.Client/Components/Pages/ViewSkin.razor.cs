using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.EventArgs;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.IO.Compression;
using System.Text;

namespace GbxTools3D.Client.Components.Pages;

public partial class ViewSkin : ComponentBase
{
    private const string ViewSkinVehiclesHide = "ViewSkinVehiclesHide";
    private const string ViewSkinInfoShow = "ViewSkinInfoShow";

    private View3D? view3d;
    private Virtualize<VehicleDto>? vehiclesVirtualize;

    private bool showVehicles = true;
    private bool showInfo = false;

    private readonly string[] extensions = ["zip"];

    [SupplyParameterFromQuery(Name = "mp")]
    private string? ManiaParkId { get; set; }

    [SupplyParameterFromQuery(Name = "game")]
    private string? GameVersion { get; set; }

    [SupplyParameterFromQuery(Name = "vehicle")]
    private string? VehicleName { get; set; }

    [SupplyParameterFromQuery(Name = "nocatalog")]
    private bool NoCatalog { get; set; }

    [SupplyParameterFromQuery(Name = "url")]
    private string? Url { get; set; }

    public GameVersion GameVersionEnum { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(ManiaParkId) && string.IsNullOrEmpty(Url);

    private MemoryStream? skinStream;

    public ZipArchive? SkinZip { get; set; }

    private string vehicleSearchValue = "";

    public string VehicleSearchValue
    {
        get => vehicleSearchValue;
        set
        {
            if (vehicleSearchValue == value)
            {
                return;
            }

            vehicleSearchValue = value;

            vehiclesVirtualize?.RefreshDataAsync();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        showVehicles = !await LocalStorage.GetItemAsync<bool>(ViewSkinVehiclesHide);
        showInfo = await LocalStorage.GetItemAsync<bool>(ViewSkinInfoShow);

        if (IsDragAndDrop)
        {
            SkinZip = GbxService.SelectedSkinZip;
            return;
        }

        string endpoint;
        if (!string.IsNullOrEmpty(Url))
        {
            endpoint = Url;
        }
        else if (!string.IsNullOrEmpty(ManiaParkId))
        {
            endpoint = $"/api/skin/mp/{ManiaParkId}";
        }
        else
        {
            throw new Exception("This should not happen");
        }

        using var response = await Http.GetAsync(endpoint);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();

        skinStream = new MemoryStream();
        await stream.CopyToAsync(skinStream);
        skinStream.Position = 0;

        SkinZip = new ZipArchive(skinStream, ZipArchiveMode.Read);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!RendererInfo.IsInteractive)
        {
            showVehicles = !await LocalStorage.GetItemAsync<bool>(ViewSkinVehiclesHide);
            showInfo = await LocalStorage.GetItemAsync<bool>(ViewSkinInfoShow);
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
            GameVersionEnum = GBX.NET.GameVersion.TMF;
        }

        await FetchInfoAsync();
    }

    private async Task FetchInfoAsync(CancellationToken cancellationToken = default)
    {
        if (GameVersionEnum == GBX.NET.GameVersion.Unspecified)
        {
            return;
        }

        await VehicleClientService.FetchAllAsync(GameVersionEnum, cancellationToken);
        if (vehiclesVirtualize is not null)
        {
            await vehiclesVirtualize.RefreshDataAsync();
        }
    }

    private Task OnUploadAsync(UploadEventArgs e)
    {
        skinStream = new MemoryStream(e.Data);
        SkinZip?.Dispose();
        SkinZip = new ZipArchive(skinStream, ZipArchiveMode.Read);
        GbxService.Add(SkinZip);
        return Task.CompletedTask;
    }

    private async Task ToggleVehiclesAsync()
    {
        showVehicles = !showVehicles;
        await LocalStorage.SetItemAsync(ViewSkinVehiclesHide, !showVehicles);
    }

    private void OnAssetSearchInput(ChangeEventArgs e)
    {
        VehicleSearchValue = e.Value?.ToString() ?? "";
    }

    private ValueTask<ItemsProviderResult<VehicleDto>> FilterVehicles(ItemsProviderRequest request)
    {
        var filteredAssets = VehicleClientService.Vehicles.Where(x => x.Name.Contains(VehicleSearchValue, StringComparison.OrdinalIgnoreCase));

        var totalCount = filteredAssets.Count();
        var assets = filteredAssets
            .Skip(request.StartIndex)
            .Take(request.Count)
            .ToList();

        return ValueTask.FromResult(new ItemsProviderResult<VehicleDto>(assets, totalCount));
    }

    private string GetViewSkinUrlQuery(GameVersion? gameVersion = null, string? vehicleName = null)
    {
        var sb = new StringBuilder();

        var first = true;

        if (!string.IsNullOrEmpty(ManiaParkId))
        {
            if (!first) sb.Append('&');
            sb.Append("mp=");
            sb.Append(ManiaParkId);
            first = false;
        }

        if (!string.IsNullOrEmpty(GameVersion))
        {
            if (!first) sb.Append('&');
            sb.Append("game=");
            sb.Append(gameVersion is null ? GameVersion : gameVersion.ToString());
            first = false;
        }

        if (!string.IsNullOrEmpty(VehicleName))
        {
            if (!first) sb.Append('&');
            sb.Append("vehicle=");
            sb.Append(vehicleName ?? VehicleName);
        }

        return sb.ToString();
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible
        SkinZip?.Dispose();

        return ValueTask.CompletedTask;
    }
}
