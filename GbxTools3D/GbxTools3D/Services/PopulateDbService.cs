using Microsoft.AspNetCore.OutputCaching;

namespace GbxTools3D.Services;

public sealed class PopulateDbService : BackgroundService
{
    private readonly IConfiguration config;
    private readonly IServiceProvider serviceProvider;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<PopulateDbService> logger;

    public PopulateDbService(
        IConfiguration config, 
        IServiceProvider serviceProvider, 
        IOutputCacheStore outputCache, 
        ILogger<PopulateDbService> logger)
    {
        this.config = config;
        this.serviceProvider = serviceProvider;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await Task.Run(async () =>
    {
        var datasetPath = config["DatasetPath"];

        if (string.IsNullOrEmpty(datasetPath))
        {
            throw new InvalidOperationException("DatasetPath is not set in configuration");
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var collectionService = scope.ServiceProvider.GetRequiredService<CollectionService>();

        await collectionService.CreateOrUpdateCollectionAsync(datasetPath, stoppingToken);
    }, stoppingToken);
}
