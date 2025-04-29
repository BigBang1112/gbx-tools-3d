namespace GbxTools3D.Configuration;

public static class CacheConfiguration
{
    public static void AddCacheServices(this IServiceCollection services)
    {
        services.AddOutputCache();
        services.AddHybridCache();
    }
}
