using GBX.NET;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Data;
using GbxTools3D.Extensions;
using Microsoft.AspNetCore.OutputCaching;

namespace GbxTools3D.Services;

internal sealed class VehicleService
{
    private readonly AppDbContext db;
    private readonly MeshService meshService;
    private readonly MaterialService materialService;
    private readonly IOutputCacheStore outputCache;

    public VehicleService(AppDbContext db, MeshService meshService, MaterialService materialService, IOutputCacheStore outputCache)
    {
        this.db = db;
        this.meshService = meshService;
        this.materialService = materialService;
        this.outputCache = outputCache;
    }

    public async Task CreateOrUpdateVehiclesAsync(string datasetPath, CancellationToken cancellationToken)
    {
        var gameVersion = GameVersion.TMF;
        var gameFolder = gameVersion.ToString();
        var gamePath = Path.Combine(datasetPath, gameFolder);

        var usedMaterials = new Dictionary<string, CPlugMaterial?>();

        foreach (var vehicleFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Vehicles", "TrackManiaVehicle"), "*.Gbx"))
        {
            var modelNode = Gbx.ParseNode<CGameItemModel>(vehicleFilePath);

            if (modelNode.Vehicle is null)
            {
                continue;
            }

            var solid = modelNode.Vehicle.GetSolid(gamePath, out var path);

            if (solid is null)
            {
                continue;
            }

            solid.PopulateUsedMaterials(usedMaterials, gamePath);

            var hash = $"GbxTools3D|Vehicle|{gameFolder}|{modelNode.Ident.Id}|WhyDidYouNotHelpMe?".Hash();

            var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path, solid,
                (modelNode.Vehicle as CSceneVehicleCar)?.VehicleStruct, cancellationToken);

            // TODO reference with vehicle
        }

        await db.SaveChangesAsync(cancellationToken);
        await outputCache.EvictByTagAsync("mesh", cancellationToken);
        
        await materialService.CreateOrUpdateMaterialsAsync(gamePath, usedMaterials, cancellationToken);
    }
}