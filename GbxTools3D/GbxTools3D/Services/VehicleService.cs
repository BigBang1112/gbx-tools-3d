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
using System.IO.Compression;

namespace GbxTools3D.Services;

internal sealed class VehicleService
{
    private readonly AppDbContext db;
    private readonly MeshService meshService;
    private readonly MaterialService materialService;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<VehicleService> logger;

    public VehicleService(
        AppDbContext db, 
        MeshService meshService, 
        MaterialService materialService, 
        IOutputCacheStore outputCache,
        ILogger<VehicleService> logger)
    {
        this.db = db;
        this.meshService = meshService;
        this.materialService = materialService;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    public async Task CreateOrUpdateVehiclesAsync(string datasetPath, CancellationToken cancellationToken)
    {
        foreach (var version in GameVersionSupport.Versions)
        {
            await CreateOrUpdateVehiclesAsync(datasetPath, version, cancellationToken);
        }
    }

    public async Task CreateOrUpdateVehiclesAsync(string datasetPath, GameVersion gameVersion, CancellationToken cancellationToken)
    {
        var gameFolder = gameVersion.ToString();
        var gamePath = Path.Combine(datasetPath, gameFolder);

        var usedMaterials = new Dictionary<string, CPlugMaterial?>();

        var vehicleFilePaths = gameVersion switch
        {
            GameVersion.TMT => Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "CanyonCE", "GameCtnObjectInfo", "Vehicles"), "*.ObjectInfo.Gbx")
                .Concat(Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "ValleyCE", "GameCtnObjectInfo", "Vehicles"), "*.ObjectInfo.Gbx"))
                .Concat(Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "LagoonCE", "GameCtnObjectInfo", "Vehicles"), "*.ObjectInfo.Gbx"))
                .Concat(Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "StadiumCE", "GameCtnObjectInfo", "Vehicles"), "*.ObjectInfo.Gbx")),
            GameVersion.MP4 => Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "Trackmania", "Items", "Vehicles"), "*.ObjectInfo.Gbx"),
            GameVersion.TMF or GameVersion.TMSX or GameVersion.TMNESWC => Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "Vehicles"), "*.Gbx"),
            _ => throw new NotSupportedException($"Game version {gameVersion} is not supported.")
        };

        foreach (var vehicleFilePath in vehicleFilePaths)
        {
            var modelNode = Gbx.ParseNode<CGameItemModel>(vehicleFilePath);

            if (gameVersion < GameVersion.MP3)
            {
                if (modelNode.Vehicle is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no vehicle data, skipping", modelNode.Ident.Id);
                    continue;
                }

                var solid = modelNode.Vehicle.GetSolid(gamePath, out var path);

                if (solid is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no solid or is corrupted, no mesh will be added", modelNode.Ident.Id);
                }
                else
                {
                    solid.PopulateUsedMaterials(usedMaterials, gamePath);

                    var hash = $"GbxTools3D|Vehicle|{gameFolder}|{modelNode.Ident.Id}|WhyDidYouNotHelpMe?".Hash();

                    var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path, solid,
                        (modelNode.Vehicle as CSceneVehicleCar)?.VehicleStruct, cancellationToken: cancellationToken);
                }
            }
            else
            {
                if (modelNode.DefaultSkinFile is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no default skin file, cannot create any solid, skipping", modelNode.Ident.Id);
                    continue;
                }

                var skinPath = modelNode.DefaultSkinFile.GetFullPath();

                // this is to avoid usage of MainBody.Mesh.gbx which has complicated bone logic
                if (gameVersion == GameVersion.MP4)
                {
                    if (skinPath.EndsWith("ValleyCarDefaultSkin.zip"))
                    {
                        skinPath = Path.Combine(Path.GetDirectoryName(skinPath)!, "ValleyCar_Original.zip");
                    }
                    else if (skinPath.EndsWith("LagoonCarDefaultSkin.zip"))
                    {
                        skinPath = Path.Combine(Path.GetDirectoryName(skinPath)!, "LagoonCar_New.zip");
                    }
                }

                using var zip = ZipFile.OpenRead(skinPath);

                var entry = zip.GetEntry("MainBodyHigh.Solid.Gbx") ?? zip.GetEntry("MainBody.Solid.Gbx")
                    ?? zip.GetEntry("MainBodyHigh.solid.gbx") ?? zip.GetEntry("MainBody.solid.gbx")
                    ?? zip.GetEntry("MainBody.Mesh.gbx");

                if (entry is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no MainBody.Mesh.gbx or MainBodyHigh.Solid.Gbx in default skin file, cannot create any solid, skipping", modelNode.Ident.Id);
                    continue;
                }

                await using var entryStream = entry.Open();
                await using var ms = new MemoryStream((int)entry.Length);
                await entryStream.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                try
                {
                    var node = await Gbx.ParseNodeAsync(ms, cancellationToken: cancellationToken);

                    var hash = $"GbxTools3D|Vehicle|{gameFolder}|{modelNode.Ident.Id}|WhyDidYouNotHelpMe?".Hash();

                    if (node is CPlugSolid solid)
                    {
                        solid.PopulateUsedMaterials(usedMaterials, gamePath);

                        var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path: null, solid,
                            vehicle: null, cancellationToken: cancellationToken);
                    }
                    else if (node is CPlugSolid2Model solid2)
                    {
                        // popuate materials manually based on these material IDs
                        // _DetailsDmg_Details
                        // _GlassDmg_Details
                        // _SkinDmg_Skin
                        // _DetailsDmg_Wheels

                        var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path: null, solid2,
                            vehicle: null, cancellationToken: cancellationToken);
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process solid for vehicle {Vehicle}", modelNode.Ident.Id);
                    continue;
                }
            }

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
            else
            {
                vehicle.CameraFar = 4.5f;
                vehicle.CameraUp = 2.2f;
                vehicle.CameraLookAtFactor = 0.88f;
                vehicle.CameraFov = 75;
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
        
        await materialService.CreateOrUpdateMaterialsAsync(gamePath, gameVersion, usedMaterials, null, cancellationToken);
    }
}