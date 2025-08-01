﻿using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Graphic;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GBX.NET.Imaging.ImageSharp;
using GbxTools3D.Client;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using GbxTools3D.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using System.Collections.Immutable;

namespace GbxTools3D.Services;

internal sealed class CollectionService
{
    private readonly AppDbContext db;
    private readonly MeshService meshService;
    private readonly MaterialService materialService;
    private readonly SoundService soundService;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<CollectionService> logger;
    
    /*private static readonly Func<AppDbContext, string, int, Task<BlockInfo?>> BlockInfoFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, string blockName, int collectionId) =>
            db.BlockInfos.Include(x => x.Icon).FirstOrDefault(x => x.Name == blockName && x.CollectionId == collectionId));
    
    private static readonly Func<AppDbContext, int, bool, int, int, Task<BlockVariant?>> BlockVariantFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, int blockInfoId, bool isGround, int i, int j) =>
            db.BlockVariants.FirstOrDefault(x => x.BlockInfoId == blockInfoId && x.Ground == isGround && x.Variant == i && x.SubVariant == j));*/

    public CollectionService(
        AppDbContext db, 
        MeshService meshService, 
        MaterialService materialService,
        SoundService soundService,
        IOutputCacheStore outputCache, 
        ILogger<CollectionService> logger)
    {
        this.db = db;
        this.meshService = meshService;
        this.materialService = materialService;
        this.soundService = soundService;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    public async Task CreateOrUpdateCollectionsAsync(string datasetPath, CancellationToken cancellationToken)
    {
        foreach (var version in GameVersionSupport.Versions)
        {
            await CreateOrUpdateCollectionsAsync(datasetPath, version, cancellationToken);
        }
    }

    public async Task CreateOrUpdateCollectionsAsync(string datasetPath, GameVersion gameVersion, CancellationToken cancellationToken)
    {
        var gameFolder = gameVersion.ToString();
        var gamePath = Path.Combine(datasetPath, gameFolder);

        if (!Directory.Exists(gamePath))
        {
            logger.LogWarning("Game folder {GameFolder} does not exist in dataset path {DatasetPath}. Skipping collection import for this version.", gameFolder, datasetPath);
            return;
        }

        foreach (var collectionFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Collections"), "*.Gbx"))
        {
            var collectionNode = Gbx.ParseNode<CGameCtnCollection>(collectionFilePath);
            
            logger.LogInformation("Checking collection {Collection}...", collectionNode.Collection);

            var collection =
                await db.Collections
                    .Include(x => x.Icon)
                    .FirstOrDefaultAsync(x => x.Name == collectionNode.Collection && x.GameVersion == gameVersion, cancellationToken);

            if (collection is null)
            {
                collection = new Collection
                {
                    Name = collectionNode.Collection ?? "",
                    GameVersion = gameVersion,
                    VehicleId = collectionNode.Vehicle?.Id ?? "",
                    VehicleCollection = collectionNode.Vehicle?.Collection ?? "",
                    VehicleAuthor = collectionNode.Vehicle?.Author ?? "",
                };

                await db.Collections.AddAsync(collection, cancellationToken);
            }

            collection.Name = collectionNode.Collection ?? "";
            collection.DisplayName = collectionNode.DisplayName == collectionNode.Collection
                ? null
                : collectionNode.DisplayName;
            collection.UpdatedAt = DateTime.UtcNow; // TODO should change only if save changes changes something
            collection.SquareHeight = (int)collectionNode.SquareHeight;
            collection.SquareSize = (int)collectionNode.SquareSize;
            collection.VehicleId = collectionNode.Vehicle?.Id ?? "";
            collection.VehicleCollection = collectionNode.Vehicle?.Collection ?? "";
            collection.VehicleAuthor = collectionNode.Vehicle?.Author ?? "";
            collection.DefaultZoneBlock = collectionNode.DefaultZoneId;
            collection.SortIndex = collectionNode.SortIndex;

            var iconTextureRelativePath = collectionNode.IconFidFile is null
                ? collectionNode.IconFullName?.Replace('\\', Path.DirectorySeparatorChar)
                : Path.GetRelativePath(gamePath, collectionNode.IconFidFile.GetFullPath());

            if (iconTextureRelativePath is not null)
            {
                if (collection.Icon is null)
                {
                    collection.Icon = new Icon();
                    await db.Icons.AddAsync(collection.Icon, cancellationToken);
                }

                collection.Icon.TexturePath = GbxPath.ChangeExtension(iconTextureRelativePath, null);
                collection.Icon.UpdatedAt = DateTime.UtcNow;

                var iconFid = await Gbx.ParseNodeAsync<CPlugBitmap>(Path.Combine(gamePath, iconTextureRelativePath), cancellationToken: cancellationToken);
                var imageFullPath = iconFid?.ImageFile?.GetFullPath();

                if (imageFullPath is not null)
                {
                    collection.Icon.ImagePath = Path.GetRelativePath(gamePath, imageFullPath);

                    byte[] data;
                    using (var image = DdsUtils.ToImageSharp(imageFullPath))
                    using (var ms = new MemoryStream())
                    {
                        await image.SaveAsWebpAsync(ms, new WebpEncoder { Method = WebpEncodingMethod.Fastest },
                            cancellationToken);
                        data = ms.ToArray();
                    }

                    collection.Icon.Data = data;
                }
            }
            else
            {
                collection.Icon = null;
            }

            if (collectionNode.IconSmallFidFile is not null)
            {
                if (collection.IconSmall is null)
                {
                    collection.IconSmall = new Icon();
                    await db.Icons.AddAsync(collection.IconSmall, cancellationToken);
                }

                collection.IconSmall.TexturePath = Path.GetRelativePath(gamePath, GbxPath.ChangeExtension(collectionNode.IconSmallFidFile.GetFullPath(), null));
                collection.IconSmall.UpdatedAt = DateTime.UtcNow;

                var imageFullPath = collectionNode.IconSmallFid?.ImageFile?.GetFullPath();

                if (imageFullPath is not null)
                {
                    collection.IconSmall.ImagePath = Path.GetRelativePath(gamePath, imageFullPath);

                    byte[] data;
                    using (var image = DdsUtils.ToImageSharp(imageFullPath))
                    using (var ms = new MemoryStream())
                    {
                        await image.SaveAsWebpAsync(ms, new WebpEncoder { Method = WebpEncodingMethod.Fastest },
                            cancellationToken);
                        data = ms.ToArray();
                    }

                    collection.IconSmall.Data = data;
                }
            }
            else
            {
                collection.IconSmall = null;
            }

            var usedMaterials = new Dictionary<string, CPlugMaterial?>();
            var addedMeshHashes = new HashSet<string>();
            var usedSounds = new Dictionary<string, Sound>();

            var terrainModifierDict = new Dictionary<string, (TerrainModifier Modifier, HashSet<string> Materials)>();

            // may have issue in TMO envs
            var folderDecoration = collectionNode.FolderDecoration ?? $"{collectionNode.Collection}\\ConstructionDecoration\\";

            foreach (var decorationFilePath in Directory.EnumerateFiles(
                Path.Combine(datasetPath, gameFolder, folderDecoration.NormalizePath()), "*.*Decoration.Gbx", SearchOption.AllDirectories))
            {
                var decorationNode =
                    (CGameCtnDecoration?)await Gbx.ParseNodeAsync(decorationFilePath,
                        cancellationToken: cancellationToken);

                if (decorationNode is null)
                {
                    continue;
                }

                var decoName = decorationNode.Ident.Id;
                
                if (decorationNode.DecoSize?.Size is null)
                {
                    throw new Exception("Decoration size is null");
                }

                var sceneName = decorationNode.DecoSize.SceneFile is null
                    ? string.Empty
                    : GbxPath.ChangeExtension(Path.GetRelativePath(gamePath, decorationNode.DecoSize.SceneFile.GetFullPath()), null);

                var decorationSize = await db.DecorationSizes.FirstOrDefaultAsync(x =>
                    x.CollectionId == collection.Id // not verified enough if it makes sense
                    && x.SizeX == decorationNode.DecoSize.Size.X && x.SizeY == decorationNode.DecoSize.Size.Y && x.SizeZ == decorationNode.DecoSize.Size.Z
                    && x.SceneName == sceneName,
                    cancellationToken);

                if (decorationSize is null)
                {
                    decorationSize = new DecorationSize
                    {
                        SizeX = decorationNode.DecoSize.Size.X,
                        SizeY = decorationNode.DecoSize.Size.Y,
                        SizeZ = decorationNode.DecoSize.Size.Z,
                        Collection = collection,
                        SceneName = sceneName
                    };
                    await db.DecorationSizes.AddAsync(decorationSize, cancellationToken);
                }

                decorationSize.BaseHeight = decorationNode.DecoSize.BaseHeightBase;
                decorationSize.OffsetBlockY = decorationNode.DecoSize.OffsetBlockY;

                if (decorationNode.DecoSize.Scene is null)
                {
                    logger.LogWarning("Decoration scene is null for {GameVersion} {Decoration}, likely due to corrupted Gbx read. Scene will be skipped, but there should be at least guess game for the assets.", gameVersion, decorationNode.Ident);
                }
                else
                {
                    var size = $"{decorationNode.DecoSize.Size.X}x{decorationNode.DecoSize.Size.Y}x{decorationNode.DecoSize.Size.Z}";
                    var scene = new List<SceneObject>();

                    var sceneObjects = decorationNode.DecoSize.Scene.Scene ?? [];
                    for (var i = 0; i < sceneObjects.Length; i++)
                    {
                        var sceneObject = sceneObjects[i];
                        var location = decorationNode.DecoSize.Scene.SceneLocations?[i].U02 ?? new Iso4();

                        if (sceneObject.Mobil is not CSceneMobil mobil)
                        {
                            continue;
                        }

                        var solid = mobil.GetSolid(gamePath, out var path);

                        if (solid is null)
                        {
                            continue;
                        }

                        solid.PopulateUsedMaterials(usedMaterials, gamePath);

                        if (path is null)
                        {
                            throw new Exception("Decoration object path is null");
                        }

                        var pathNoExt = GbxPath.ChangeExtension(path, null);

                        scene.Add(new SceneObject
                        {
                            Solid = pathNoExt,
                            Location = location
                        });

                        var solidHash = $"GbxTools3D|Decoration|{gameFolder}|{collection.Name}|{pathNoExt}|Je te hais".Hash();

                        if (!addedMeshHashes.Add(solidHash))
                        {
                            continue;
                        }

                        await meshService.GetOrCreateMeshAsync(gamePath, solidHash, path, solid, vehicle: null, isDeco: true, cancellationToken: cancellationToken);
                    }

                    var lights = decorationNode.DecoSize.Scene.Lights ?? [];
                    for (var i = 0; i < lights.Length; i++)
                    {
                        var light = lights[i];
                        var location = decorationNode.DecoSize.Scene.LightLocations?[i].U02 ?? new Iso4();

                        if (light.Node is null)
                        {
                            continue;
                        }

                        var type = light.Node.Light?.MainGxLight switch
                        {
                            GxLightAmbient => "Ambient",
                            GxLightDirectional => "Directional",
                            null => throw new Exception("Light is null"),
                            _ => throw new Exception($"Unknown light type ({light.Node.Light.MainGxLight.GetType()})")
                        };

                        var gxLight = light.Node.Light.MainGxLight;
                        var gxLightAmbient = gxLight as GxLightAmbient;

                        scene.Add(new SceneObject
                        {
                            Light = new Light
                            {
                                Type = type,
                                IsActive = light.Node.IsActive,
                                Color = gxLight.Color,
                                Intensity = gxLight.Intensity,
                                FlareIntensity = gxLight.FlareIntensity,
                                ShadowIntensity = gxLight.ShadowIntensity,
                                ShadeMinY = gxLightAmbient?.ShadeMinY,
                                ShadeMaxY = gxLightAmbient?.ShadeMaxY,
                            },
                            Location = location
                        });
                    }

                    var objects = decorationNode.DecoSize.Scene.Objects ?? [];
                    for (var i = 0; i < objects.Length; i++)
                    {
                        var sceneObject = objects[i];
                        var location = decorationNode.DecoSize.Scene.ObjectLocations?[i] ?? new Iso4();

                        if (sceneObject is CSceneMobil mobil)
                        {
                            var solid = mobil.GetSolid(gamePath, out var path);

                            if (solid is null)
                            {
                                continue;
                            }

                            solid.PopulateUsedMaterials(usedMaterials, gamePath);

                            if (path is null)
                            {
                                throw new Exception("Decoration object path is null");
                            }

                            var pathNoExt = GbxPath.ChangeExtension(path, null);

                            scene.Add(new SceneObject
                            {
                                Solid = pathNoExt,
                                Location = location
                            });

                            var solidHash = $"GbxTools3D|Decoration|{gameFolder}|{collection.Name}|{pathNoExt}|Je te hais".Hash();

                            if (!addedMeshHashes.Add(solidHash))
                            {
                                continue;
                            }

                            await meshService.GetOrCreateMeshAsync(gamePath, solidHash, path, solid, vehicle: null, isDeco: true, cancellationToken: cancellationToken);
                        }
                        else if (sceneObject is CSceneLight light)
                        {
                            var type = light.Light?.MainGxLight switch
                            {
                                GxLightAmbient => "Ambient",
                                GxLightDirectional => "Directional",
                                null => throw new Exception("Light is null"),
                                _ => throw new Exception($"Unknown light type ({light.Light.MainGxLight.GetType()})")
                            };

                            var gxLight = light.Light.MainGxLight;
                            var gxLightAmbient = gxLight as GxLightAmbient;

                            scene.Add(new SceneObject
                            {
                                Light = new Light
                                {
                                    Type = type,
                                    IsActive = light.IsActive,
                                    Color = gxLight.Color,
                                    Intensity = gxLight.Intensity,
                                    FlareIntensity = gxLight.FlareIntensity,
                                    ShadowIntensity = gxLight.ShadowIntensity,
                                    ShadeMinY = gxLightAmbient?.ShadeMinY,
                                    ShadeMaxY = gxLightAmbient?.ShadeMaxY,
                                },
                                Location = location
                            });
                        }
                    }

                    decorationSize.Scene = scene.ToImmutableArray();
                }

                var decoration = await db.Decorations
                    .FirstOrDefaultAsync(x => x.DecorationSize == decorationSize && x.Name == decoName, cancellationToken);
                
                if (decoration is null)
                {
                    decoration = new Decoration
                    {
                        Name = decoName,
                        DecorationSize = decorationSize,
                    };
                    await db.Decorations.AddAsync(decoration, cancellationToken);
                }

                if (decorationNode.DecoAudio is not null)
                {
                    var musics = ImmutableDictionary.CreateBuilder<string, string>();
                    var sounds = ImmutableDictionary.CreateBuilder<string, string>();
                    
                    foreach (var (key, extNode) in decorationNode.DecoAudio.Musics ?? [])
                    {
                        if (extNode.File is null)
                        {
                            continue;
                        }
                        
                        var fullFileWithoutExtension = GbxPath.ChangeExtension(extNode.File.GetFullPath(), null);
                        musics[key] = Path.GetRelativePath(gamePath, fullFileWithoutExtension);

                        if (extNode.Node is null)
                        {
                            continue;
                        }

                        await soundService.CreateOrUpdateSoundAsync(
                            gamePath,
                            gameVersion,
                            extNode.Node,
                            musics[key],
                            usedSounds,
                            cancellationToken);
                    }
                    
                    foreach (var (key, extNode) in decorationNode.DecoAudio.Sounds ?? [])
                    {
                        if (extNode.File is null)
                        {
                            continue;
                        }

                        var fullFileWithoutExtension = GbxPath.ChangeExtension(extNode.File.GetFullPath(), null);
                        sounds[key] = Path.GetRelativePath(gamePath, fullFileWithoutExtension);

                        if (extNode.Node is null)
                        {
                            continue;
                        }

                        await soundService.CreateOrUpdateSoundAsync(
                            gamePath,
                            gameVersion,
                            extNode.Node,
                            sounds[key],
                            usedSounds,
                            cancellationToken);
                    }
                    
                    decoration.Musics = musics.ToImmutable();
                    decoration.Sounds = sounds.ToImmutable();
                }

                if (decorationNode.DecoMood is not null)
                {
                    decoration.Remap = decorationNode.DecoMood.RemapFolder;
                }

                if (decorationNode.TerrainModifierCovered is null && decorationNode.TerrainModifierCoveredFile is not null)
                {
                    logger.LogWarning("Decoration terrain modifier covered is null ({File}) in collection {Collection}. It may be corrupted or not supported.", decorationNode.TerrainModifierCoveredFile, collection.Name);
                }

                if (decorationNode.TerrainModifierCovered is not null && decorationNode.TerrainModifierCoveredFile is not null)
                {
                    decoration.TerrainModifierCovered = await ProcessTerrainModifierAsync(
                        decorationNode.TerrainModifierCovered, 
                        decorationNode.TerrainModifierCoveredFile, 
                        terrainModifierDict, 
                        collection, 
                        cancellationToken);
                }
            }
            
            logger.LogInformation("Checking block changes...");

            var zoneDict = collectionNode.CompleteListZoneList?.ToDictionary(zone => zone.Node switch
            {
                CGameCtnZoneFrontier frontier => frontier.BlockInfoFrontier?.Ident.Id ?? Guid.NewGuid().ToString() /* throw new Exception("BlockInfoFrontier is null")*/,
                CGameCtnZoneFlat flat => flat.BlockInfoFlat?.Ident.Id ?? Guid.NewGuid().ToString() /*throw new Exception("BlockInfoFlat is null") some zones are corrupted */,
                CGameCtnZoneTransition transition => transition.BlockInfoTransition?.Ident.Id ?? Guid.NewGuid().ToString() /*?? throw new Exception("BlockInfoTransition is null") some zones are corrupted */,
                _ => Guid.NewGuid().ToString()
            }, x => x.Node) ?? [];

            foreach (var terrainModifierExt in collectionNode.ReplacementTerrainModifiers ?? [])
            {
                if (terrainModifierExt.Node is null && terrainModifierExt.File is not null)
                {
                    logger.LogWarning("Terrain modifier node is null ({File}) in collection {Collection}. It may be corrupted or not supported.", terrainModifierExt.File, collection.Name);
                    continue;
                }

                if (terrainModifierExt.Node is null || terrainModifierExt.File is null)
                {
                    continue;
                }

                await ProcessTerrainModifierAsync(terrainModifierExt.Node, terrainModifierExt.File, terrainModifierDict, collection, cancellationToken);
            }

            var terrainModifierLookup = terrainModifierDict
                .SelectMany(kvp => kvp.Value.Materials.Select(material => new { material, kvp.Value.Modifier }))
                .ToLookup(x => x.material, x => x.Modifier);

            var blockInfos = await db.BlockInfos
                .Include(x => x.Icon)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.Mesh)
                .Include(x => x.TerrainModifier)
                .Where(x => x.CollectionId == collection.Id)
                .ToDictionaryAsync(x => x.Name, cancellationToken);

            // may have issue in TMO envs
            var folderBlockInfo = collectionNode.FolderBlockInfo ?? $"{collectionNode.Collection}\\ConstructionBlockInfo\\";

            foreach (var blockInfoFilePath in Directory.EnumerateFiles(
                         Path.Combine(datasetPath, gameFolder, folderBlockInfo.NormalizePath()), "*.Gbx",
                         SearchOption.AllDirectories))
            {
                CGameCtnBlockInfo? blockInfoNode;

                try
                {
                    blockInfoNode = (CGameCtnBlockInfo?)await Gbx.ParseNodeAsync(blockInfoFilePath, cancellationToken: cancellationToken);
                }
                catch
                {
                    logger.LogWarning("Failed to parse block info {BlockInfoFilePath}. It may be corrupted or not supported.", blockInfoFilePath);
                    continue;
                }

                if (blockInfoNode is null)
                {
                    continue;
                }

                var blockName = blockInfoNode.Ident.Id;
                
                if (blockName.Length > 96)
                {
                    throw new Exception($"Block name {blockName} is too long");
                }

                var blockInfo = blockInfos.GetValueOrDefault(blockName);

                if (blockInfo is null)
                {
                    blockInfo = new BlockInfo
                    {
                        Collection = collection,
                        Name = blockName,
                    };
                    await db.BlockInfos.AddAsync(blockInfo, cancellationToken);
                }

                blockInfo.Name = blockName;
                blockInfo.AirUnits = gameVersion >= GameVersion.TMT
                    ? blockInfoNode.VariantBaseAir?.BlockUnitModels?.Select(UnitInfoToBlockUnit).ToImmutableArray() ?? []
                    : blockInfoNode.AirBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToImmutableArray() ?? [];
                blockInfo.GroundUnits = gameVersion >= GameVersion.TMT
                    ? blockInfoNode.VariantBaseGround?.BlockUnitModels?.Select(UnitInfoToBlockUnit).ToImmutableArray() ?? []
                    : blockInfoNode.GroundBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToImmutableArray() ?? [];
                blockInfo.HasAirHelper = blockInfoNode.AirHelperMobil is not null;
                blockInfo.HasGroundHelper = blockInfoNode.GroundHelperMobil is not null;
                blockInfo.HasConstructionModeHelper = blockInfoNode.ConstructionModeHelperMobil is not null;
                blockInfo.HasGroundWaypoint = blockInfoNode.VariantBaseGround?.WaypointTriggerSolid is not null;
                blockInfo.HasAirWaypoint = blockInfoNode.VariantBaseAir?.WaypointTriggerSolid is not null;
                blockInfo.IsRoad = blockInfoNode is CGameCtnBlockInfoRoad;

                if (zoneDict.TryGetValue(blockName, out var zone) && zone is not null)
                {
                    blockInfo.Height = (byte)zone.Height;

                    if (zone is CGameCtnZoneFlat flat)
                    {
                        blockInfo.PylonName = flat.BlockInfoPylon?.Ident.Id;
                    }
                }

                blockInfo.SpawnLocAir = blockInfoNode.SpawnLocAir ?? (blockInfoNode.VariantBaseAir is null ? Iso4.Identity
                    : new Iso4(0, 0, 0, 0, 0, 0, 0, 0, 0, blockInfoNode.VariantBaseAir.SpawnTrans.X, blockInfoNode.VariantBaseAir.SpawnTrans.Y, blockInfoNode.VariantBaseAir.SpawnTrans.Z));
                blockInfo.SpawnLocGround = blockInfoNode.SpawnLocGround ?? (blockInfoNode.VariantBaseGround is null ? Iso4.Identity
                    : new Iso4(0, 0, 0, 0, 0, 0, 0, 0, 0, blockInfoNode.VariantBaseGround.SpawnTrans.X, blockInfoNode.VariantBaseGround.SpawnTrans.Y, blockInfoNode.VariantBaseGround.SpawnTrans.Z));

                await ProcessOldBlockVariantsAsync(blockInfoNode.AirMobils, gamePath, gameVersion, collection.Name, blockName,
                    isGround: false, blockInfo, usedMaterials, usedSounds, cancellationToken);
                await ProcessOldBlockVariantsAsync(blockInfoNode.GroundMobils, gamePath, gameVersion, collection.Name, blockName,
                    isGround: true, blockInfo, usedMaterials, usedSounds, cancellationToken);

                await ProcessNewBlockVariantsAsync(blockInfoNode.VariantBaseAir, gamePath, gameVersion, collection.Name, blockName,
                    isGround: false, blockInfo, usedMaterials, usedSounds, cancellationToken);
                await ProcessNewBlockVariantsAsync(blockInfoNode.VariantBaseGround, gamePath, gameVersion, collection.Name, blockName,
                    isGround: true, blockInfo, usedMaterials, usedSounds, cancellationToken);

                // Helpers cause a ton of mesh duplicates, but they shouldn't be impactful much

                if (blockInfoNode.AirHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.AirHelperMobil,
                        gamePath, 
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|False|You're not helper here >:(",
                        cancellationToken);
                }
                else if (blockInfoNode.VariantBaseAir?.HelperSolidFid is not null)
                {
                    await GetOrCreateMeshFromSolidFidAsync(
                        blockInfoNode.VariantBaseAir.HelperSolidFid,
                        blockInfoNode.VariantBaseAir.HelperSolidFidFile,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|False|You're not helper here >:(",
                        cancellationToken);
                }

                if (blockInfoNode.GroundHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.GroundHelperMobil,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|True|You're not helper here >:(",
                        cancellationToken);
                }
                else if (blockInfoNode.VariantBaseGround?.HelperSolidFid is not null)
                {
                    await GetOrCreateMeshFromSolidFidAsync(
                        blockInfoNode.VariantBaseGround.HelperSolidFid,
                        blockInfoNode.VariantBaseGround.HelperSolidFidFile,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|True|You're not helper here >:(",
                        cancellationToken);
                }

                if (blockInfoNode.ConstructionModeHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.ConstructionModeHelperMobil,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|Auris or Aurimas? WirtuaL. Adrien wtf is this timing",
                        cancellationToken);
                }

                if (blockInfoNode.VariantBaseAir?.WaypointTriggerSolid is CPlugSolid waypointTriggerSolidAir)
                {
                    await GetOrCreateMeshFromSolidFidAsync(
                        waypointTriggerSolidAir,
                        blockInfoNode.VariantBaseAir.WaypointTriggerSolidFile,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|False|Way to go bois",
                        cancellationToken);
                }
                if (blockInfoNode.VariantBaseGround?.WaypointTriggerSolid is CPlugSolid waypointTriggerSolidGround)
                {
                    await GetOrCreateMeshFromSolidFidAsync(
                        waypointTriggerSolidGround,
                        blockInfoNode.VariantBaseGround.WaypointTriggerSolidFile,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{collection.Name}|{blockName}|True|Way to go bois",
                        cancellationToken);
                }

                using var iconMs = new MemoryStream();
                if (await blockInfoNode.ExportIconAsync(iconMs, new WebpEncoder { FileFormat = WebpFileFormatType.Lossless }, cancellationToken))
                {
                    if (blockInfo.Icon is null)
                    {
                        blockInfo.Icon = new Icon();
                        await db.Icons.AddAsync(blockInfo.Icon, cancellationToken);
                    }

                    blockInfo.Icon.Data = iconMs.ToArray();
                    blockInfo.Icon.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    blockInfo.Icon = null;
                }

                blockInfo.TerrainModifier = blockName switch
                {
                    "StadiumDirt" => terrainModifierDict["TerrainModifierDirt"].Modifier,
                    "RallyRockyGrass" => terrainModifierDict["TerrainModifierRockyGrass"].Modifier,
                    "AlpineTundra" => terrainModifierDict["TerrainModifierTundra"].Modifier,
                    "SpeedRock" => terrainModifierDict["TerrainModifierRock"].Modifier,
                    _ => null
                };
            }

            logger.LogInformation("Saving collection changes ({Collection})...", collection.Name);
            await db.SaveChangesAsync(cancellationToken);
            await outputCache.EvictByTagAsync("mesh", cancellationToken);
            await outputCache.EvictByTagAsync("block", cancellationToken);
            await outputCache.EvictByTagAsync("decoration", cancellationToken);
            
            await materialService.CreateOrUpdateMaterialsAsync(gamePath, gameVersion, usedMaterials, terrainModifierLookup, cancellationToken);
        }
        
        logger.LogInformation("Collections complete!");
    }

    private async Task<TerrainModifier?> ProcessTerrainModifierAsync(
        CGameCtnDecorationTerrainModifier terrainModifier,
        GbxRefTableFile terrainModifierFile, 
        Dictionary<string, (TerrainModifier Modifier, HashSet<string> Materials)> terrainModifierDict, 
        Collection collection,
        CancellationToken cancellationToken)
    {
        if (terrainModifier is null)
        {
            logger.LogWarning("Terrain modifier node is null ({File}) in collection {Collection}. It may be corrupted or not supported.", terrainModifierFile, collection.Name);
            return null;
        }

        if (terrainModifier.Remapping is null)
        {
            logger.LogWarning("Terrain modifier remapping is null ({File}) in collection {Collection}. It may be corrupted or not supported.", terrainModifier.RemappingFile, collection.Name);
            return null;
        }

        if (terrainModifier.IdName is null)
        {
            logger.LogWarning("Terrain modifier IdName is null ({File}) in collection {Collection}.", terrainModifierFile, collection.Name);
            return null;
        }

        var materials = terrainModifier.Remapping.CustomizableFids?.Select(x => x.Name).ToHashSet() ?? [];
        var remapFolder = terrainModifier.RemapFolder ?? throw new Exception("Terrain modifier remap folder is null");

        var terrainModifierModel = await db.TerrainModifiers
            .FirstOrDefaultAsync(x => x.Name == terrainModifier.IdName
                && x.CollectionId == collection.Id, cancellationToken: cancellationToken);

        if (terrainModifierModel is null)
        {
            terrainModifierModel = new TerrainModifier
            {
                Name = terrainModifier.IdName,
                Collection = collection,
                RemapFolder = remapFolder,
            };
            await db.TerrainModifiers.AddAsync(terrainModifierModel, cancellationToken);
        }

        terrainModifierModel.RemapFolder = remapFolder;

        terrainModifierDict[terrainModifier.IdName] = (terrainModifierModel, materials);

        return terrainModifierModel;
    }

    private async Task ProcessOldBlockVariantsAsync(
        External<CSceneMobil>[][]? mobils,
        string gamePath,
        GameVersion gameVersion,
        string collectionName,
        string blockName,
        bool isGround,
        BlockInfo blockInfo,
        Dictionary<string, CPlugMaterial?> usedMaterials,
        Dictionary<string, Sound> usedSounds,
        CancellationToken cancellationToken)
    {
        if (mobils is null)
        {
            return;
        }

        var gameFolder = gameVersion.ToString();

        for (var i = 0; i < mobils.Length; i++)
        {
            for (var j = 0; j < mobils[i].Length; j++)
            {
                var mobil = mobils[i][j];
                var variant = mobil.Node;

                if (variant is null)
                {
                    continue;
                }

                var variantPath = mobil.File is null
                    ? null
                    : Path.GetRelativePath(gamePath, mobil.File.GetFullPath());

                var solid = variant.GetSolid(gamePath, out var path);

                if (solid is null)
                {
                    continue;
                }

                solid.PopulateUsedMaterials(usedMaterials, gamePath);

                var hash = $"GbxTools3D|Solid|{gameFolder}|{collectionName}|{blockName}|{isGround}MyGuy|{i}|{j}|PleaseDontAbuseThisThankYou:*".Hash();

                var mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path, solid, vehicle: null,
                    cancellationToken: cancellationToken);

                var blockVariant = blockInfo.Variants.FirstOrDefault(x => x.Ground == isGround && x.Variant == i && x.SubVariant == j);

                if (blockVariant is null)
                {
                    blockVariant = new BlockVariant
                    {
                        BlockInfo = blockInfo,
                        Mesh = mesh,
                    };

                    await db.BlockVariants.AddAsync(blockVariant, cancellationToken);
                }

                blockVariant.BlockInfo = blockInfo;
                blockVariant.Ground = isGround;
                blockVariant.Variant = (byte)i;
                blockVariant.SubVariant = (byte)j;
                blockVariant.Mesh = mesh;
                blockVariant.Path = variantPath;

                var k = 0;
                foreach (var link in variant.ObjectLink ?? [])
                {
                    if (link.Mobil is null)
                    {
                        continue;
                    }
                    
                    var objectLinkSolid = link.Mobil.GetSolid(gamePath, out var objectLinkSolidPath);
                    
                    if (objectLinkSolid is null)
                    {
                        continue;
                    }
                    
                    solid.PopulateUsedMaterials(usedMaterials, gamePath);

                    var solidHash = $"GbxTools3D|Solid|{gameFolder}|{collectionName}|{blockName}|Hella{isGround}|{i}|{j}|{k}|marosisPakPakGhidraGang".Hash();
                    
                    var objectLinkMesh = await meshService.GetOrCreateMeshAsync(gamePath, solidHash, objectLinkSolidPath, objectLinkSolid, vehicle: null,
                        cancellationToken: cancellationToken);
                    
                    var objectLink = await db.ObjectLinks
                        .Include(x => x.Variant)
                        .FirstOrDefaultAsync(x => x.Variant.Id == blockVariant.Id && x.Index == k, cancellationToken);
                    
                    if (objectLink is null)
                    {
                        objectLink = new ObjectLink
                        {
                            Variant = blockVariant,
                            Mesh = objectLinkMesh,
                            Index = k,
                        };
                        await db.ObjectLinks.AddAsync(objectLink, cancellationToken);
                    }
                    
                    objectLink.Variant = blockVariant;
                    objectLink.Index = k;
                    objectLink.Path = link.MobilFile is null
                        ? null
                        : Path.GetRelativePath(gamePath, link.MobilFile.GetFullPath());
                    objectLink.Loc = link.RelativeLocation;

                    var soundLink = link.Mobil.ObjectLink?.FirstOrDefault(x => x.Object is CSceneSoundSource { SoundSource: not null });

                    if (soundLink is not null)
                    {
                        objectLink.Sound = await ProcessSoundLinkAsync(gamePath, gameVersion, soundLink, usedSounds, cancellationToken);
                        objectLink.SoundLoc = soundLink.RelativeLocation;
                    }

                    k++;
                }
            }
        }
    }

    private async Task ProcessNewBlockVariantsAsync(
        CGameCtnBlockInfoVariant? variant,
        string gamePath,
        GameVersion gameVersion,
        string collectionName,
        string blockName,
        bool isGround,
        BlockInfo blockInfo,
        Dictionary<string, CPlugMaterial?> usedMaterials,
        Dictionary<string, Sound> usedSounds,
        CancellationToken cancellationToken)
    {
        if (variant?.Mobils is null)
        {
            return;
        }

        var gameFolder = gameVersion.ToString();

        for (var i = 0; i < variant.Mobils.Length; i++)
        {
            for (var j = 0; j < variant.Mobils[i].Length; j++)
            {
                var mobil = variant.Mobils[i][j];

                if (mobil is null)
                {
                    continue;
                }

                var hash = $"GbxTools3D|Solid|{gameFolder}|{collectionName}|{blockName}|{isGround}MyGuy|{i}|{j}|PleaseDontAbuseThisThankYou:*".Hash();

                Mesh mesh;
                if (mobil.SolidFid is CPlugSolid solid)
                {
                    var path = mobil.SolidFidFile is null
                        ? null
                        : Path.GetRelativePath(gamePath, mobil.SolidFidFile.GetFullPath());

                    solid.PopulateUsedMaterials(usedMaterials, gamePath);

                    mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path, solid, vehicle: null, cancellationToken: cancellationToken);
                }
                else if (mobil.PrefabFid is CPlugPrefab prefab)
                {
                    var path = mobil.PrefabFidFile is null
                        ? null
                        : Path.GetRelativePath(gamePath, mobil.PrefabFidFile.GetFullPath());

                    prefab.PopulateUsedMaterials(usedMaterials, gamePath);

                    mesh = await meshService.GetOrCreateMeshAsync(gamePath, hash, path, prefab, cancellationToken: cancellationToken);
                }
                else
                {
                    continue;
                }

                var blockVariant = blockInfo.Variants.FirstOrDefault(x => x.Ground == isGround && x.Variant == i && x.SubVariant == j);

                if (blockVariant is null)
                {
                    blockVariant = new BlockVariant
                    {
                        BlockInfo = blockInfo,
                        Mesh = mesh,
                    };

                    await db.BlockVariants.AddAsync(blockVariant, cancellationToken);
                }

                blockVariant.BlockInfo = blockInfo;
                blockVariant.Ground = isGround;
                blockVariant.Variant = (byte)i;
                blockVariant.SubVariant = (byte)j;
                blockVariant.Mesh = mesh;
            }
        }
    }

    private async Task<Sound?> ProcessSoundLinkAsync(
        string gamePath,
        GameVersion gameVersion,
        CSceneObjectLink soundLink,
        Dictionary<string, Sound> usedSounds,
        CancellationToken cancellationToken)
    {
        var soundSource = ((CSceneSoundSource)soundLink.Object!).SoundSource!;

        var sound = await soundService.CreateOrUpdateSoundAsync(
            gamePath,
            gameVersion,
            soundSource,
            usedSounds,
            cancellationToken);

        if (sound is null)
        {
            return null;
        }

        return sound;
    }

    private static BlockUnit UnitInfoToBlockUnit(CGameCtnBlockUnitInfo unit)
    {
        return new BlockUnit
        {
            Offset = (Byte3)unit.RelativeOffset,
            Clips = unit.Clips?.Any(x => x.Node is not null) == true
                ? unit.Clips.Select((x, i) => new BlockClip
                {
                    Dir = (ClipDir)i,
                    Id = x.Node?.Ident.Id ?? ""
                }).Where(x => !string.IsNullOrEmpty(x.Id)).ToImmutableArray()
                : null,
            AcceptPylons = unit.AcceptPylons == 255 ? null : (byte)unit.AcceptPylons,
            PlacePylons = unit.PlacePylons == 0 ? null : (byte)unit.PlacePylons,
            TerrainModifier = string.IsNullOrEmpty(unit.TerrainModifierId) ? null : unit.TerrainModifierId
        };
    }

    private async ValueTask<Mesh?> GetOrCreateMeshFromMobilAsync(CSceneMobil mobil, string relativeTo, string toHash, CancellationToken cancellationToken)
    {
        var solid = mobil.GetSolid(relativeTo, out var path);

        if (solid is null)
        {
            return null;
        }
        
        return await meshService.GetOrCreateMeshAsync(relativeTo, toHash.Hash(), path, solid, vehicle: null, cancellationToken: cancellationToken);
    }

    private async ValueTask<Mesh?> GetOrCreateMeshFromSolidFidAsync(CPlugSolid solid, GbxRefTableFile? solidFile, string relativeTo, string toHash, CancellationToken cancellationToken)
    {
        var path = solidFile is null ? null : Path.GetRelativePath(relativeTo, solidFile.GetFullPath());
        return await meshService.GetOrCreateMeshAsync(relativeTo, toHash.Hash(), path, solid, vehicle: null, cancellationToken: cancellationToken);
    }

    public async Task<Collection?> GetAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken)
    {
        return await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == collectionName, cancellationToken);
    }
}