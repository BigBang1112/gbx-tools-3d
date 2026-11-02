using Microsoft.AspNetCore.Components;
using Reuniverse.Razor;

namespace GbxTools3D.Client.Components;

public partial class UploadArea : ComponentBase
{
    [Parameter]
    public int? Height { get; set; } = 100;

    [Parameter]
    public int FontSize { get; set; } = 100;

    /// <summary>
    /// Extensions without the dot.
    /// </summary>
    [Parameter]
    public string[]? Extensions { get; set; }

    [Parameter]
    public EventCallback<UploadEventArgs> OnUpload { get; set; }
}
