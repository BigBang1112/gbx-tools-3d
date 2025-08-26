using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

public sealed class PopulateDbService : BackgroundService
{
    private readonly IConfiguration config;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PopulateDbService> logger;

    public PopulateDbService(
        IConfiguration config, 
        IServiceScopeFactory scopeFactory, 
        ILogger<PopulateDbService> logger)
    {
        this.config = config;
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await Task.Run(async () =>
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.DataImports.AnyAsync(stoppingToken))
        {
            logger.LogInformation("Data already imported. Run import manually.");
            return;
        }

        var datasetPath = config["DatasetPath"];

        if (string.IsNullOrEmpty(datasetPath))
        {
            throw new InvalidOperationException("DatasetPath is not set in configuration");
        }

        var vehicleService = scope.ServiceProvider.GetRequiredService<VehicleService>();
        var collectionService = scope.ServiceProvider.GetRequiredService<CollectionService>();
        var campaignService = scope.ServiceProvider.GetRequiredService<CampaignService>();

        await campaignService.CreateOrUpdateCampaignsAsync(datasetPath, GBX.NET.GameVersion.TMF, CancellationToken.None);
        await vehicleService.CreateOrUpdateVehiclesAsync(datasetPath, stoppingToken);
        await collectionService.CreateOrUpdateCollectionsAsync(datasetPath, stoppingToken);

        await db.DataImports.AddAsync(new DataImport(), stoppingToken);
        await db.SaveChangesAsync(stoppingToken);
    }, stoppingToken);
}
