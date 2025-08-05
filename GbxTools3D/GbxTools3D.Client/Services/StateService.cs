using GbxTools3D.Client.Dtos;

namespace GbxTools3D.Client.Services;

public class StateService
{
    public event Action<LoadingStageDto>? OnTasksDefined;

    public void NotifyTasksDefined(LoadingStageDto message)
    {
        OnTasksDefined?.Invoke(message);
    }
    
    public event Action<LoadingStageDto>? OnTasksChanged;

    public void NotifyTasksChanged(LoadingStageDto message)
    {
        OnTasksChanged?.Invoke(message);
    }
}
