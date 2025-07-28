using GbxTools3D.Client.Models;
using Microsoft.AspNetCore.Components;

namespace GbxTools3D.Client.Components.Modules;

public partial class RenderInfo : ComponentBase
{
    private const string ModuleRenderInfoHide = "ModuleRenderInfoHide";

    private bool show;
    private RenderDetails? renderDetails;

    public void Update(RenderDetails renderDetails)
    {
        this.renderDetails = renderDetails;
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        if (RendererInfo.IsInteractive)
        {
            show = !await LocalStorage.GetItemAsync<bool>(ModuleRenderInfoHide);
        }
    }

    private async Task ToggleShowAsync()
    {
        show = !show;
        await LocalStorage.SetItemAsync(ModuleRenderInfoHide, !show);
    }
}
