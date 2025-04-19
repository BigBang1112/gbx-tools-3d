using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace GbxTools3D.Client.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services, IWebAssemblyHostEnvironment hostEnvironment)
    {
        services.AddScoped(sp =>
            new HttpClient
            {
                BaseAddress = new Uri(hostEnvironment.BaseAddress)
            });
        services.AddSingleton<GbxService>();
        services.AddScoped<ICollectionClientService, CollectionClientService>();
        services.AddScoped<IBlockClientService, BlockClientService>();
        services.AddScoped<IDecorationClientService, DecorationClientService>();
        services.AddScoped<IVehicleClientService, VehicleClientService>();
    }
}
