using GbxTools3D.Client.EventArgs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GbxTools3D.Client.Components;

public partial class UploadArea : ComponentBase
{
    private ElementReference inputFile;
    private IJSObjectReference? module;
    private DotNetObjectReference<UploadArea>? objRef;

    private bool dragged;
    private string? uploadFileName;

    [Parameter]
    public int? Height { get; set; } = 100;

    [Parameter]
    public int FontSize { get; set; } = 100;

    /// <summary>
    /// Extensions without the dot.
    /// </summary>
    [Parameter]
    public required string[]? Extensions { get; set; }

    [Parameter]
    public EventCallback<UploadEventArgs> OnUpload { get; set; }

    protected override void OnInitialized() =>
        objRef = DotNetObjectReference.Create(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", $"./Components/UploadArea.razor.js");
            await module.InvokeVoidAsync("addHandlers", inputFile, objRef);
        }
    }

    [JSInvokable]
    public async Task UploadAsync(string fileName, byte[] data)
    {
        await OnUpload.InvokeAsync(new UploadEventArgs(fileName, data));
    }

    public void SetUploadFileName(string fileName)
    {
        uploadFileName = fileName;
    }

    [JSInvokable]
    public void ClearUploadFileName()
    {
        uploadFileName = null;
    }

    private void DragEnter(DragEventArgs e)
    {
        dragged = true;
    }

    private void DragEnd()
    {
        dragged = false;
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
