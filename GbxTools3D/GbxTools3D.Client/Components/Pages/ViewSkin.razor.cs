using GBX.NET;
using GbxTools3D.Client.EventArgs;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components;
using System.IO.Compression;

namespace GbxTools3D.Client.Components.Pages;

public partial class ViewSkin : ComponentBase
{
    private View3D? view3d;

    private readonly string[] extensions = ["zip"];

    [SupplyParameterFromQuery(Name = "mp")]
    private string? ManiaParkId { get; set; }

    [SupplyParameterFromQuery(Name = "game")]
    private string? GameVersion { get; set; }

    [SupplyParameterFromQuery(Name = "vehicle")]
    private string? VehicleName { get; set; }

    public GameVersion GameVersionEnum { get; set; }

    public bool IsDragAndDrop => string.IsNullOrEmpty(ManiaParkId);

    private MemoryStream? skinStream;

    public ZipArchive? SkinZip { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (IsDragAndDrop)
        {
            SkinZip = GbxService.SelectedSkinZip;
            return;
        }

        string endpoint;
        if (!string.IsNullOrEmpty(ManiaParkId))
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

    protected override void OnParametersSet()
    {
        if (Enum.TryParse(GameVersion, true, out GameVersion gameVersion))
        {
            GameVersionEnum = gameVersion;
        }
        else
        {
            GameVersionEnum = GBX.NET.GameVersion.TMF;
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

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible
        SkinZip?.Dispose();

        return ValueTask.CompletedTask;
    }
}
