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
using System.Collections.Immutable;
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

        var fakeShadShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, "FakeShad", null, shader: null, cancellationToken);
        var detailsShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, "Details", null, shader: null, cancellationToken);
        var skinShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, "Skin", null, shader: null, cancellationToken);
        var wheelsShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, "Wheels", null, shader: null, cancellationToken);

        foreach (var vehicleFilePath in vehicleFilePaths)
        {
            var modelNode = Gbx.ParseNode<CGameItemModel>(vehicleFilePath);
            var vehicleName = modelNode.Ident.Id;

            if (gameVersion < GameVersion.MP3)
            {
                if (modelNode.Vehicle is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no vehicle data, skipping", vehicleName);
                    continue;
                }

                var solid = modelNode.Vehicle.GetSolid(gamePath, out var path);

                if (solid is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no solid or is corrupted, no mesh will be added", vehicleName);
                }
                else
                {
                    solid.PopulateUsedMaterials(usedMaterials, gamePath);

                    var hash = $"GbxTools3D|Vehicle|{gameFolder}|{vehicleName}|WhyDidYouNotHelpMe?".Hash();

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

                var solidEntry = zip.GetEntry("MainBodyHigh.Solid.Gbx") ?? zip.GetEntry("MainBody.Solid.Gbx")
                    ?? zip.GetEntry("MainBodyHigh.solid.gbx") ?? zip.GetEntry("MainBody.solid.gbx")
                    ?? zip.GetEntry("MainBody.Mesh.gbx");

                if (solidEntry is null)
                {
                    logger.LogWarning("Vehicle {Vehicle} has no MainBody.Mesh.gbx or MainBodyHigh.Solid.Gbx in default skin file, cannot create any solid, skipping", modelNode.Ident.Id);
                    continue;
                }

                var fakeShadMat = await CreateCustomMaterialAsync(gameVersion, "FakeShad", zip.GetEntry("FakeShad.dds") ?? zip.GetEntry("ProjShad.dds"), vehicleName, fakeShadShader, cancellationToken);
                var detailsMat = await CreateCustomMaterialAsync(gameVersion, "Details", zip.GetEntry("DetailsDiffuse.dds") ?? zip.GetEntry("Details.dds"), vehicleName, detailsShader, cancellationToken);
                //await CreateCustomMaterialAsync(gameVersion, zip, "Icon.dds", vehicleName, cancellationToken); should fall into Icons than Textures
                var skinMat = await CreateCustomMaterialAsync(gameVersion, "Skin", zip.GetEntry("SkinDiffuse.dds") ?? zip.GetEntry("Diffuse.dds"), vehicleName, skinShader, cancellationToken);
                var wheelsMat = await CreateCustomMaterialAsync(gameVersion, "Wheels", zip.GetEntry("WheelsDiffuse.dds") ?? zip.GetEntry("Wheels.dds"), vehicleName, wheelsShader, cancellationToken);

                var materialMapping = new Dictionary<string, string>();

                if (fakeShadMat is not null)
                {
                    materialMapping["FakeShad"] = fakeShadMat.Name;
                }

                if (detailsMat is not null)
                {
                    materialMapping["dBody"] = detailsMat.Name;
                }

                if (skinMat is not null)
                {
                    materialMapping["sBody"] = skinMat.Name;
                    materialMapping["gBody"] = skinMat.Name;
                }


                await using var solidEntryStream = solidEntry.Open();
                await using var ms = new MemoryStream((int)solidEntry.Length);
                await solidEntryStream.CopyToAsync(ms, cancellationToken);
                ms.Position = 0;

                try
                {
                    var node = await Gbx.ParseNodeAsync(ms, cancellationToken: cancellationToken);

                    var hash = $"GbxTools3D|Vehicle|{gameFolder}|{vehicleName}|WhyDidYouNotHelpMe?".Hash();

                    if (node is CPlugSolid solid)
                    {
                        // solid.PopulateUsedMaterials(usedMaterials, gamePath); not needed because the materials are not defined by ref table file in the zip skins

                        var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path: null, solid,
                            vehicle: null, materialSpecialMapping: materialMapping, cancellationToken: cancellationToken);
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
                    logger.LogError(ex, "Failed to process solid for vehicle {Vehicle}", vehicleName);
                    continue;
                }
            }

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
                // default values for majority of unresolved cases
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

    private async Task<Material?> CreateCustomMaterialAsync(
        GameVersion gameVersion,
        string materialName,
        ZipArchiveEntry? diffuseEntry,
        string vehicleName, 
        Material shader,
        CancellationToken cancellationToken)
    {
        if (diffuseEntry is null)
        {
            logger.LogWarning("Vehicle {Vehicle} has no diffuse texture for material {MaterialName}, skipping", vehicleName, materialName);
            return null;
        }

        var diffuseFileName = diffuseEntry.Name;

        using var diffuseStream = diffuseEntry.Open();

        byte[] data;

        using (var image = DdsUtils.ToImageSharp(diffuseStream))
        {
            data = await materialService.OptimizeImageAsync(image, "Diffuse", diffuseFileName, cancellationToken);
        }

        var diffuseTextureName = $":{vehicleName}_{diffuseFileName}";

        logger.LogInformation("Creating diffuse texture {DiffuseTextureName}...", diffuseTextureName);
        await MaterialService.CreateOrUpdateTextureAsync(db, diffuseFileName, gameVersion, diffuseTextureName, data, cancellationToken);

        var actualMaterialName = $":{vehicleName}_{materialName}";

        logger.LogInformation("Creating material {MaterialName}...", actualMaterialName);

        var textures = ImmutableDictionary.CreateBuilder<string, string>();
        textures.Add("Diffuse", diffuseTextureName);

        return await materialService.AddOrUpdateMaterialAsync(gameVersion, actualMaterialName, textures.ToImmutable(), shader, cancellationToken);
    }
}