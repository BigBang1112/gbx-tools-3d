using GbxTools3D.Client.Services;

namespace GbxTools3D.Client.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddSingleton<GbxService>();
    }
}
