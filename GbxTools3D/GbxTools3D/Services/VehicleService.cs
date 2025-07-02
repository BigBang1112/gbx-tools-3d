using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Engines.TrackMania;
using GBX.NET.Imaging.ImageSharp;
using GbxTools3D.Client;
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
            GameVersion.TMF or GameVersion.TMSX or GameVersion.TMNESWC => Directory.GetFiles(Path.Combine(datasetPath, gameFolder, "Vehicles", "TrackManiaVehicle"), "*.Gbx"),
            _ => throw new NotSupportedException($"Game version {gameVersion} is not supported.")
        };

        // TODO: not needed run on all game versions
        var fakeShadShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":FakeShad", null, shader: null, cancellationToken);
        var detailsShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Details", null, shader: null, cancellationToken);
        var skinShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Skin", null, shader: null, cancellationToken);
        var wheelsShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Wheels", null, shader: null, cancellationToken);
        var glassShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Glass", null, shader: null, cancellationToken);
        var lightShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Light", null, shader: null, cancellationToken);
        var pilotShader = await materialService.AddOrUpdateMaterialAsync(gameVersion, ":Pilot", null, shader: null, cancellationToken);

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
                var pilotMat = await CreateCustomMaterialAsync(gameVersion, "Pilot", zip.GetEntry("Pilot.dds"), vehicleName, pilotShader, cancellationToken);

                var materialMapping = new Dictionary<string, string>();

                if (fakeShadMat is not null)
                {
                    materialMapping["FakeShad"] = fakeShadMat.Name;
                }

                if (detailsMat is not null)
                {
                    materialMapping["dFLGuard"] = detailsMat.Name;
                    materialMapping["dFRGuard"] = detailsMat.Name;
                    materialMapping["dRRGuard"] = detailsMat.Name;
                    materialMapping["dRLGuard"] = detailsMat.Name;
                    materialMapping["dRLWheel"] = detailsMat.Name;
                    materialMapping["dRRWheel"] = detailsMat.Name;
                    materialMapping["dRDoor"] = detailsMat.Name;
                    materialMapping["dLDoor"] = detailsMat.Name;
                    materialMapping["dHood"] = detailsMat.Name;
                    materialMapping["dTrunk"] = detailsMat.Name;
                    materialMapping["dBody"] = detailsMat.Name;
                    materialMapping["dFRWheel"] = detailsMat.Name;
                    materialMapping["dFLWheel"] = detailsMat.Name;
                    materialMapping["dRRArmTop"] = detailsMat.Name;
                    materialMapping["dRRCardan"] = detailsMat.Name;
                    materialMapping["dRRArmBot"] = detailsMat.Name;
                    materialMapping["dRRHub"] = detailsMat.Name;
                    materialMapping["dRLArmTop"] = detailsMat.Name;
                    materialMapping["dRLCardan"] = detailsMat.Name;
                    materialMapping["dRLArmBot"] = detailsMat.Name;
                    materialMapping["dRLHub"] = detailsMat.Name;
                    materialMapping["dRLSusp"] = detailsMat.Name;
                    materialMapping["dFRArmTop"] = detailsMat.Name;
                    materialMapping["dFRArmDir"] = detailsMat.Name;
                    materialMapping["dFRArmBot"] = detailsMat.Name;
                    materialMapping["dFRHub"] = detailsMat.Name;
                    materialMapping["dFRSusp"] = detailsMat.Name;
                    materialMapping["dFLSusp"] = detailsMat.Name;
                    materialMapping["dFLArmTop"] = detailsMat.Name;
                    materialMapping["dFLArmDir"] = detailsMat.Name;
                    materialMapping["dFLArmBot"] = detailsMat.Name;
                    materialMapping["dFLHub"] = detailsMat.Name;
                    materialMapping["dEngines"] = detailsMat.Name;
                    materialMapping["dSupportM"] = detailsMat.Name;
                    materialMapping["dRLSusp"] = detailsMat.Name;
                    materialMapping["dRRSusp"] = detailsMat.Name;
                    materialMapping["dExhaust"] = detailsMat.Name;
                    materialMapping["dExhaustF"] = detailsMat.Name;
                    materialMapping["dFront"] = detailsMat.Name;
                    materialMapping["dGasDoor"] = detailsMat.Name;
                    materialMapping["dRearA"] = detailsMat.Name;
                    materialMapping["dNos"] = detailsMat.Name;
                    materialMapping["dSeat"] = detailsMat.Name;
                    materialMapping["dGaz"] = detailsMat.Name;
                    materialMapping["dSpeedC"] = detailsMat.Name;
                    materialMapping["dCacheM"] = detailsMat.Name;
                    materialMapping["dFRCardan"] = detailsMat.Name;
                    materialMapping["dFLCardan"] = detailsMat.Name;
                    materialMapping["dFrontligh"] = detailsMat.Name;
                    materialMapping["dNitrous"] = detailsMat.Name;
                    materialMapping["dElecEng"] = detailsMat.Name;
                    materialMapping["dBodyS"] = detailsMat.Name;
                }

                if (skinMat is not null)
                {
                    materialMapping["sRDoor"] = skinMat.Name;
                    materialMapping["sFLWheel"] = skinMat.Name;
                    materialMapping["sTrunk"] = skinMat.Name;
                    materialMapping["sFRWheel"] = skinMat.Name;
                    materialMapping["sHood"] = skinMat.Name;
                    materialMapping["sLDoor"] = skinMat.Name;
                    materialMapping["sRRWheel"] = skinMat.Name;
                    materialMapping["sRLWheel"] = skinMat.Name;
                    materialMapping["sBody"] = skinMat.Name;
                    materialMapping["sPilHead"] = skinMat.Name;
                    materialMapping["sFLHub"] = skinMat.Name;
                    materialMapping["sFRHub"] = skinMat.Name;
                    materialMapping["sRLHub"] = skinMat.Name;
                    materialMapping["sRRHub"] = skinMat.Name;
                    materialMapping["sFLGuard"] = skinMat.Name;
                    materialMapping["sFRGuard"] = skinMat.Name;
                    materialMapping["sAileronF"] = skinMat.Name;
                    materialMapping["sAileronB"] = skinMat.Name;
                    materialMapping["sSecure"] = skinMat.Name;
                    materialMapping["sAirvent"] = skinMat.Name;
                    materialMapping["sFLArmTop"] = skinMat.Name;
                    materialMapping["sFLArmBot"] = skinMat.Name;
                    materialMapping["sRLArmTop"] = skinMat.Name;
                    materialMapping["sRLArmBot"] = skinMat.Name;
                    materialMapping["sFRArmTop"] = skinMat.Name;
                    materialMapping["sFRArmBot"] = skinMat.Name;
                    materialMapping["sRRArmTop"] = skinMat.Name;
                    materialMapping["sRRArmBot"] = skinMat.Name;
                    materialMapping["sFLArmDir"] = skinMat.Name;
                    materialMapping["sFRArmDir"] = skinMat.Name;
                    materialMapping["sBodyF"] = skinMat.Name;
                    materialMapping["sBodyAir"] = skinMat.Name;
                    materialMapping["sTrunkFx"] = skinMat.Name;
                    materialMapping["sFAileron"] = skinMat.Name;
                    materialMapping["sBodyS"] = skinMat.Name;
                }

                if (wheelsMat is not null)
                {
                    materialMapping["wFLWheel"] = wheelsMat.Name;
                    materialMapping["wFRWheel"] = wheelsMat.Name;
                    materialMapping["wRRWheel"] = wheelsMat.Name;
                    materialMapping["wRLWheel"] = wheelsMat.Name;
                    materialMapping["wPilot"] = wheelsMat.Name;
                    materialMapping["wpilot"] = wheelsMat.Name;
                    materialMapping["wBodyS"] = wheelsMat.Name;
                }

                if (glassShader is not null)
                {
                    materialMapping["gTrunk"] = glassShader.Name;
                    materialMapping["gLDoor"] = glassShader.Name;
                    materialMapping["gRDoor"] = glassShader.Name;
                    materialMapping["gBody"] = glassShader.Name;
                    materialMapping["gFWShield"] = glassShader.Name;
                    materialMapping["gRWShield"] = glassShader.Name;
                    materialMapping["gFront"] = glassShader.Name;
                    materialMapping["gRearA"] = glassShader.Name;
                    materialMapping["gFrontligh"] = glassShader.Name;
                }

                if (lightShader is not null)
                {
                    materialMapping["FLLight"] = lightShader.Name;
                    materialMapping["FRLight"] = lightShader.Name;
                    materialMapping["RLLight"] = lightShader.Name;
                    materialMapping["RRLight"] = lightShader.Name;
                    materialMapping["LightFProj"] = lightShader.Name;
                    materialMapping["Exhaust1"] = lightShader.Name;
                    materialMapping["Exhaust2"] = lightShader.Name;
                    materialMapping["Exhaust3"] = lightShader.Name;
                    materialMapping["Exhaust4"] = lightShader.Name;
                }

                if (pilotMat is not null)
                {
                    materialMapping["pBody"] = pilotMat.Name;
                    materialMapping["pFLWheel"] = pilotMat.Name;
                    materialMapping["pFRWheel"] = pilotMat.Name;
                    materialMapping["pRLWheel"] = pilotMat.Name;
                    materialMapping["pRRWheel"] = pilotMat.Name;
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