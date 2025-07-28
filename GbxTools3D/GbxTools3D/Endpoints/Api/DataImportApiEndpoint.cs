using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GbxTools3D.Endpoints.Api;

public abstract class DataImportApiEndpoint
{
    private static Task? importTask;

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", ImportData);
        group.MapGet("/status", ImportDataStatus).RequireAuthorization();
    }

    private static Results<Accepted, Conflict, UnauthorizedHttpResult> ImportData(
        [FromHeader(Name = "X-Key")] string key,
        IServiceProvider serviceProvider, 
        IConfiguration config, 
        ILogger<DataImportApiEndpoint> logger,
        CancellationToken cancellationToken)
    {
        if (key != "GE1zo5awcJDBm6j")
        {
            return TypedResults.Unauthorized();
        }

        var datasetPath = config["DatasetPath"];

        if (string.IsNullOrEmpty(datasetPath))
        {
            throw new InvalidOperationException("DatasetPath is not set in configuration");
        }

        if (importTask is not null)
        {
            return TypedResults.Conflict();
        }

        importTask = Task.Run(async () =>
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var vehicleService = scope.ServiceProvider.GetRequiredService<VehicleService>();
                var collectionService = scope.ServiceProvider.GetRequiredService<CollectionService>();

                await vehicleService.CreateOrUpdateVehiclesAsync(datasetPath, CancellationToken.None);
                await collectionService.CreateOrUpdateCollectionsAsync(datasetPath, CancellationToken.None);

                await db.DataImports.AddAsync(new DataImport());
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Data import failed");
                throw;
            }
            finally
            {
                importTask = null;
            }
        }, cancellationToken);

        return TypedResults.Accepted(uri: "/api/dataimport/status");
    }

    private static Ok ImportDataStatus()
    {
        return TypedResults.Ok();
    }
}