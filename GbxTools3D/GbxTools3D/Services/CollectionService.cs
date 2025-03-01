using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Graphic;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GbxTools3D.Client.Extensions;
using GbxTools3D.Client.Models;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using GbxTools3D.Extensions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class CollectionService
{
    private readonly AppDbContext db;
    private readonly MeshService meshService;
    private readonly MaterialService materialService;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<CollectionService> logger;
    
    private static readonly Func<AppDbContext, string, int, Task<BlockInfo?>> BlockInfoFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, string blockName, int collectionId) =>
            db.BlockInfos.FirstOrDefault(x => x.Name == blockName && x.CollectionId == collectionId));
    
    private static readonly Func<AppDbContext, int, bool, int, int, Task<BlockVariant?>> BlockVariantFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, int blockInfoId, bool isGround, int i, int j) =>
            db.BlockVariants.FirstOrDefault(x => x.BlockInfoId == blockInfoId && x.Ground == isGround && x.Variant == i && x.SubVariant == j));

    public CollectionService(
        AppDbContext db, 
        MeshService meshService, 
        MaterialService materialService, 
        IOutputCacheStore outputCache, 
        ILogger<CollectionService> logger)
    {
        this.db = db;
        this.meshService = meshService;
        this.materialService = materialService;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    public async Task CreateOrUpdateCollectionsAsync(string datasetPath, CancellationToken cancellationToken)
    {
        var gameVersion = GameVersion.TMF;
        var gameFolder = gameVersion.ToString();
        var gamePath = Path.Combine(datasetPath, gameFolder);

        foreach (var collectionFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Collections"), "*.Gbx"))
        {
            var collectionNode = Gbx.ParseNode<CGameCtnCollection>(collectionFilePath);
            
            logger.LogInformation("Checking collection {Collection}...", collectionNode.Collection);

            var collection =
                await db.Collections.FirstOrDefaultAsync(x => x.Name == collectionNode.Collection, cancellationToken);

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

            var usedMaterials = new Dictionary<string, CPlugMaterial?>();
            var addedMeshHashes = new HashSet<string>();

            foreach (var decorationFilePath in Directory.EnumerateFiles(
                Path.Combine(datasetPath, gameFolder, collectionNode.FolderDecoration!), "*.Gbx"))
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
                
                var decorationSize = await db.DecorationSizes.FirstOrDefaultAsync(x =>
                    x.CollectionId == collection.Id && // not verified enough if it makes sense
                    x.SizeX == decorationNode.DecoSize.Size.X && x.SizeY == decorationNode.DecoSize.Size.Y && x.SizeZ == decorationNode.DecoSize.Size.Z,
                    cancellationToken);

                if (decorationSize is null)
                {
                    decorationSize = new DecorationSize
                    {
                        SizeX = decorationNode.DecoSize.Size.X,
                        SizeY = decorationNode.DecoSize.Size.Y,
                        SizeZ = decorationNode.DecoSize.Size.Z,
                        Collection = collection
                    };
                    await db.DecorationSizes.AddAsync(decorationSize, cancellationToken);
                }

                decorationSize.BaseHeight = decorationNode.DecoSize.BaseHeightBase;
                
                if (decorationNode.DecoSize.Scene is null)
                {
                    throw new Exception("Decoration scene is null");
                }
                
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

                    path = GbxPath.ChangeExtension(path, null);
                    
                    scene.Add(new SceneObject
                    {
                        Solid = path,
                        Location = location
                    });

                    var solidHash = $"GbxTools3D|Decoration|{gameFolder}|{collection.Name}|{size}|{path}|Je te hais".Hash();

                    if (!addedMeshHashes.Add(solidHash))
                    {
                        continue;
                    }
                    
                    await meshService.GetOrCreateMeshAsync(solidHash, path, solid, vehicle: null, cancellationToken);
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
                
                decorationSize.Scene = scene.ToArray();

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
                    var musics = new Dictionary<string, string>();
                    var sounds = new Dictionary<string, string>();
                    
                    foreach (var (key, extNode) in decorationNode.DecoAudio.Musics ?? [])
                    {
                        if (extNode.File is null)
                        {
                            continue;
                        }
                        
                        var fullFileWithoutExtension = Path.ChangeExtension(Path.ChangeExtension(extNode.File.GetFullPath(), null), null);
                        musics[key] = Path.GetRelativePath(gamePath, fullFileWithoutExtension);
                    }
                    
                    foreach (var (key, extNode) in decorationNode.DecoAudio.Sounds ?? [])
                    {
                        if (extNode.File is null)
                        {
                            continue;
                        }

                        var fullFileWithoutExtension = Path.ChangeExtension(Path.ChangeExtension(extNode.File.GetFullPath(), null), null);
                        sounds[key] = Path.GetRelativePath(gamePath, fullFileWithoutExtension);
                    }
                    
                    decoration.Musics = musics;
                    decoration.Sounds = sounds;
                }

                if (decorationNode.DecoMood is not null)
                {
                    decoration.Remap = decorationNode.DecoMood.RemapFolder;
                }
            }
            
            logger.LogInformation("Checking block changes...");
            
            var zoneDict = collectionNode.CompleteListZoneList?.ToDictionary(zone => zone.Node switch
            {
                CGameCtnZoneFrontier frontier => frontier.BlockInfoFrontier?.Ident.Id ??
                                                 throw new Exception("BlockInfoFrontier is null"),
                CGameCtnZoneFlat flat => flat.BlockInfoFlat?.Ident.Id ??
                                         throw new Exception("BlockInfoFlat is null"),
                _ => throw new Exception("Unknown zone type")
            }, x => x.Node ?? throw new Exception("Zone node is null")) ?? [];

            foreach (var blockInfoFilePath in Directory.EnumerateFiles(
                         Path.Combine(datasetPath, gameFolder, collectionNode.FolderBlockInfo!), "*.Gbx",
                         SearchOption.AllDirectories))
            {
                var blockInfoNode =
                    (CGameCtnBlockInfo?)await Gbx.ParseNodeAsync(blockInfoFilePath,
                        cancellationToken: cancellationToken);

                if (blockInfoNode is null)
                {
                    continue;
                }

                var blockName = blockInfoNode.Ident.Id;
                
                if (blockName.Length > 64)
                {
                    throw new Exception($"Block name {blockName} is too long");
                }

                var blockInfo = await BlockInfoFirstOrDefaultAsync(db, blockName, collection.Id);
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
                blockInfo.AirUnits = blockInfoNode.AirBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];
                blockInfo.GroundUnits = blockInfoNode.GroundBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];
                blockInfo.HasAirHelper = blockInfoNode.AirHelperMobil is not null;
                blockInfo.HasGroundHelper = blockInfoNode.GroundHelperMobil is not null;
                blockInfo.HasConstructionModeHelper = blockInfoNode.ConstructionModeHelperMobil is not null;

                if (zoneDict.TryGetValue(blockName, out var zone))
                {
                    blockInfo.Height = (byte)zone.Height;
                }

                await ProcessBlockVariantsAsync(blockInfoNode.AirMobils, gamePath, gameFolder, blockName,
                    isGround: false, blockInfo, usedMaterials, cancellationToken);
                await ProcessBlockVariantsAsync(blockInfoNode.GroundMobils, gamePath, gameFolder, blockName,
                    isGround: true, blockInfo, usedMaterials, cancellationToken);

                // Helpers cause a ton of mesh duplicates, but they shouldn't be impactful much
                
                if (blockInfoNode.AirHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.AirHelperMobil,
                        gamePath, 
                        $"GbxTools3D|Solid|{gameFolder}|{blockName}|False|You're not helper here >:(",
                        cancellationToken);
                }

                if (blockInfoNode.GroundHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.GroundHelperMobil,
                        gamePath, 
                        $"GbxTools3D|Solid|{gameFolder}|{blockName}|True|You're not helper here >:(",
                        cancellationToken);
                }

                if (blockInfoNode.ConstructionModeHelperMobil is not null)
                {
                    await GetOrCreateMeshFromMobilAsync(blockInfoNode.ConstructionModeHelperMobil,
                        gamePath,
                        $"GbxTools3D|Solid|{gameFolder}|{blockName}|Auris or Aurimas? WirtuaL",
                        cancellationToken);
                }
            }

            logger.LogInformation("Saving collection changes ({Collection})...", collection.Name);
            await db.SaveChangesAsync(cancellationToken);
            await outputCache.EvictByTagAsync("mesh", cancellationToken);
            await outputCache.EvictByTagAsync("block", cancellationToken);
            await outputCache.EvictByTagAsync("decoration", cancellationToken);
            
            await materialService.CreateOrUpdateMaterialsAsync(gamePath, usedMaterials, cancellationToken);
        }
        
        logger.LogInformation("Collections complete!");
    }

    private async Task ProcessBlockVariantsAsync(
        External<CSceneMobil>[][]? mobils,
        string gamePath,
        string gameFolder,
        string blockName,
        bool isGround,
        BlockInfo blockInfo,
        Dictionary<string, CPlugMaterial?> usedMaterials,
        CancellationToken cancellationToken)
    {
        if (mobils is null)
        {
            return;
        }

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

                var hash = $"GbxTools3D|Solid|{gameFolder}|{blockName}|{isGround}MyGuy|{i}|{j}|PleaseDontAbuseThisThankYou:*".Hash();

                var mesh = await meshService.GetOrCreateMeshAsync(hash, path, solid, vehicle: null,
                    cancellationToken);

                var blockVariant = await BlockVariantFirstOrDefaultAsync(db, blockInfo.Id, isGround, i, j);

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

                    var solidHash = $"GbxTools3D|Solid|{gameFolder}|{blockName}|Hella{isGround}|{i}|{j}|{k}|marosisPakPakGhidraGang".Hash();
                    
                    var objectLinkMesh = await meshService.GetOrCreateMeshAsync(solidHash, objectLinkSolidPath, objectLinkSolid, vehicle: null,
                        cancellationToken);
                    
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
                    objectLink.XX = link.RelativeLocation.XX;
                    objectLink.XY = link.RelativeLocation.XY;
                    objectLink.XZ = link.RelativeLocation.XZ;
                    objectLink.YX = link.RelativeLocation.YX;
                    objectLink.YY = link.RelativeLocation.YY;
                    objectLink.YZ = link.RelativeLocation.YZ;
                    objectLink.ZX = link.RelativeLocation.ZX;
                    objectLink.ZY = link.RelativeLocation.ZY;
                    objectLink.ZZ = link.RelativeLocation.ZZ;
                    objectLink.TX = link.RelativeLocation.TX;
                    objectLink.TY = link.RelativeLocation.TY;
                    objectLink.TZ = link.RelativeLocation.TZ;

                    k++;
                }
            }
        }
    }

    private static BlockUnit UnitInfoToBlockUnit(CGameCtnBlockUnitInfo unit)
    {
        return new BlockUnit
        {
            Offset = unit.RelativeOffset,
            Clips = unit.Clips?.Any(x => x.Node is not null) == true
                ? unit.Clips.Select((x, i) => new BlockClip
                {
                    Dir = (ClipDir)i,
                    Id = x.Node?.Ident.Id ?? ""
                }).Where(x => !string.IsNullOrEmpty(x.Id)).ToArray()
                : null,
            AcceptPylons = unit.AcceptPylons == 255 ? null : (byte)unit.AcceptPylons,
            PlacePylons = unit.PlacePylons == 0 ? null : (byte)unit.PlacePylons,
        };
    }

    private async Task<Mesh?> GetOrCreateMeshFromMobilAsync(CSceneMobil mobil, string relativeTo, string toHash, CancellationToken cancellationToken)
    {
        var solid = mobil.GetSolid(relativeTo, out var path);

        if (solid is null)
        {
            return null;
        }
        
        return await meshService.GetOrCreateMeshAsync(toHash.Hash(), path, solid, vehicle: null, cancellationToken);
    }

    public async Task<Collection?> GetAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken)
    {
        return await db.Collections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameVersion == gameVersion && x.Name == collectionName, cancellationToken);
    }
}