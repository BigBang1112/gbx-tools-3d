namespace GbxTools3D.Configuration;

public static class CacheConfiguration
{
    public static void AddCacheServices(this IServiceCollection services)
    {
        services.AddOutputCache();
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10 MB
        });
    }
}
