using GbxTools3D.Client.Models;

namespace GbxTools3D.Client.Services;

public class StateService
{
    public event Action<LoadingStage>? OnLoadingDefined;

    public void NotifyLoadingDefined(LoadingStage message)
    {
        OnLoadingDefined?.Invoke(message);
    }
    
    public event Action<LoadingStage>? OnLoadingChanged;

    public void NotifyLoadingChanged(LoadingStage message)
    {
        OnLoadingChanged?.Invoke(message);
    }
}
