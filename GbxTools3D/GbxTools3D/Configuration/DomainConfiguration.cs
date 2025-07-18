using GbxTools3D.Client.Services;
using GbxTools3D.Services;

namespace GbxTools3D.Configuration;

public static class DomainConfiguration
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddHostedService<PopulateDbService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<CollectionService>();
        services.AddScoped<ICollectionClientService, CollectionClientService>();
        services.AddScoped<IBlockClientService, BlockClientService>();
        services.AddScoped<IDecorationClientService, DecorationClientService>();
        services.AddScoped<IVehicleClientService, VehicleClientService>();
        services.AddScoped<MaterialService>();
        services.AddScoped<MeshService>();
        services.AddScoped<SoundService>();
        services.AddScoped<ShowcaseService>();

        // client-sided services
        services.AddSingleton<GbxService>();
    }
}
