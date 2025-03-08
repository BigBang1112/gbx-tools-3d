using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Engines.TrackMania;
using GBX.NET.Imaging.ImageSharp;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp.Formats.Webp;

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

            var vehicleName = modelNode.Ident.Id;

            var vehicle = await db.Vehicles
                .Include(x => x.Icon)
                .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == vehicleName, cancellationToken);

            if (vehicle is null)
            {
                vehicle = new Vehicle
                {
                    GameVersion = gameVersion,
                    Name = vehicleName,
                };
                await db.Vehicles.AddAsync(vehicle, cancellationToken);
            }

            if (modelNode.Cameras?.ElementAtOrDefault(1)?.Node is CGameControlCameraTarget camera2)
            {
                if (camera2 is CGameControlCameraTrackManiaRace3 cam)
                {
                    vehicle.CameraFar = cam.Far;
                    vehicle.CameraUp = cam.Up;
                }
                else if (camera2 is CGameControlCameraTrackManiaRace oldCam)
                {
                    vehicle.CameraFar = (oldCam.CarCameraDistance + oldCam.CarCameraAlign) / 2; // honestly pure hack I dont get it
                    vehicle.CameraUp = oldCam.CarCameraHeight;
                }

                vehicle.CameraLookAtFactor = camera2.LookAtFactor;
                vehicle.CameraFov = camera2.Fov;
            }

            using var iconMs = new MemoryStream();
            if (await modelNode.ExportIconAsync(iconMs, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless }, cancellationToken))
            {
                if (vehicle.Icon is null)
                {
                    vehicle.Icon = new Icon();
                    await db.Icons.AddAsync(vehicle.Icon, cancellationToken);
                }

                vehicle.Icon.Data = iconMs.ToArray();
                vehicle.Icon.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                vehicle.Icon = null;
            }

            // TODO reference with vehicle
        }

        await db.SaveChangesAsync(cancellationToken);
        await outputCache.EvictByTagAsync("mesh", cancellationToken);
        
        await materialService.CreateOrUpdateMaterialsAsync(gamePath, usedMaterials, cancellationToken);
    }
}