using GBX.NET;
using GBX.NET.Engines.Plug;
using GbxTools3D.Client.EventArgs;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Pages;

public partial class ViewMesh : ComponentBase
{
    private readonly string[] extensions = ["Mesh.Gbx", "Solid.Gbx", "Solid2.Gbx", "Prefab.Gbx"];

    private CPlugSolid? solid;
    private CPlugSolid2Model? solid2;
    private CPlugPrefab? prefab;

    protected override void OnInitialized()
    {
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        switch (GbxService.SelectedMesh?.Node)
        {
            case CPlugSolid solid:
                this.solid = solid;
                break;
            case CPlugSolid2Model solid2:
                this.solid2 = solid2;
                break;
            case CPlugPrefab prefab:
                this.prefab = prefab;
                break;
        }
    }

    private async Task OnUploadAsync(UploadEventArgs e)
    {
        using var ms = new MemoryStream(e.Data);
        var gbx = await Gbx.ParseAsync(ms);

        switch (gbx)
        {
            case Gbx<CPlugSolid> solid:
                GbxService.Add(solid);
                this.solid = solid.Node;
                break;
            case Gbx<CPlugSolid2Model> solid2:
                GbxService.Add(solid2);
                this.solid2 = solid2.Node;
                break;
            case Gbx<CPlugPrefab> prefab:
                GbxService.Add(prefab);
                this.prefab = prefab.Node;
                break;
        }
    }

    public ValueTask DisposeAsync()
    {
        GbxService.Deselect(); // can be more flexible

        return ValueTask.CompletedTask;
    }
}
