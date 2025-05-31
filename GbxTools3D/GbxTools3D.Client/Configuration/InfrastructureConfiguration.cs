using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace GbxTools3D.Client.Configuration;

public static class InfrastructureConfiguration
{
    public static void AddInfrastructureServices(this IServiceCollection services, IWebAssemblyHostEnvironment hostEnvironment)
    {
        services.AddScoped(sp =>
            new HttpClient
            {
                BaseAddress = new Uri(hostEnvironment.BaseAddress)
            });
        services.AddScoped<ICollectionClientService, CollectionClientService>();
        services.AddScoped<IBlockClientService, BlockClientService>();
        services.AddScoped<IDecorationClientService, DecorationClientService>();
        services.AddScoped<IVehicleClientService, VehicleClientService>();
    }
}
