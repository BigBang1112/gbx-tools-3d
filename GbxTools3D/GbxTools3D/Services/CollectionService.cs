using System.Security.Cryptography;
using System.Text;
using GBX.NET;
using GBX.NET.Components;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GbxTools3D.Client.Models;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class CollectionService
{
    private readonly AppDbContext db;
    private readonly MeshService meshService;
    private readonly IOutputCacheStore outputCache;
    
    private static readonly Func<AppDbContext, string, int, Task<BlockInfo?>> BlockInfoFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, string blockName, int collectionId) =>
            db.BlockInfos.FirstOrDefault(x => x.Name == blockName && x.CollectionId == collectionId));
    
    private static readonly Func<AppDbContext, int, bool, int, int, Task<BlockVariant?>> BlockVariantFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, int blockInfoId, bool isGround, int i, int j) =>
            db.BlockVariants.FirstOrDefault(x => x.BlockInfoId == blockInfoId && x.Ground == isGround && x.Variant == i && x.SubVariant == j));

    public CollectionService(AppDbContext db, MeshService meshService, IOutputCacheStore outputCache)
    {
        this.db = db;
        this.meshService = meshService;
        this.outputCache = outputCache;
    }

    public async Task CreateOrUpdateCollectionAsync(string datasetPath, CancellationToken cancellationToken)
    {
        var gameVersion = GameVersion.TMF;
        var gameFolder = gameVersion.ToString();

        var usedMaterials = new Dictionary<string, CPlugMaterial?>();

        // not quite collection related
        foreach (var vehicleFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Vehicles", "TrackManiaVehicle"), "*.Gbx"))
        {
            var modelNode = Gbx.ParseNode<CGameItemModel>(vehicleFilePath);

            if (modelNode.Vehicle is null)
            {
                continue;
            }

            var solid = GetSolidFromMobil(modelNode.Vehicle, Path.Combine(datasetPath, gameFolder), out var path);
            
            if (solid is null)
            {
                continue;
            }
            
            PopulateUsedMaterials(usedMaterials, solid, Path.Combine(datasetPath, gameFolder));

            var hash = HashStr($"GbxTools3D|Vehicle|{gameFolder}|{modelNode.Ident.Id}|WhyDidYouNotHelpMe?");

            var mesh = await meshService.GetOrCreateMeshAsync(hash, path, solid,
                (modelNode.Vehicle as CSceneVehicleCar)?.VehicleStruct, cancellationToken);

            // TODO reference with vehicle
        }

        foreach (var collectionFilePath in Directory.EnumerateFiles(
                     Path.Combine(datasetPath, gameFolder, "Collections"), "*.Gbx"))
        {
            var collectionNode = Gbx.ParseNode<CGameCtnCollection>(collectionFilePath);

            var collection =
                await db.Collections.FirstOrDefaultAsync(x => x.Name == collectionNode.Collection, cancellationToken);

            if (collection is null)
            {
                collection = new Collection
                {
                    Name = collectionNode.Collection ?? "",
                    DisplayName = collectionNode.DisplayName == collectionNode.Collection ? null : collectionNode.DisplayName,
                    GameVersion = gameVersion
                };

                await db.Collections.AddAsync(collection, cancellationToken);
            }

            collection.Name = collectionNode.Collection ?? "";
            collection.UpdatedAt = DateTime.UtcNow;

            var zoneDict = collectionNode.CompleteListZoneList?.ToDictionary(zone => zone.Node switch
            {
                CGameCtnZoneFrontier frontier => frontier.BlockInfoFrontier?.Ident.Id ??
                                                 throw new Exception("BlockInfoFrontier is null"),
                CGameCtnZoneFlat flat => flat.BlockInfoFlat?.Ident.Id ??
                                         throw new Exception("BlockInfoFlat is null"),
                _ => throw new Exception("Unknown zone type")
            }, x => x.Node ?? throw new Exception("Zone node is null")) ?? [];

            foreach (var decorationFilePath in Directory.EnumerateFiles(
                Path.Combine(datasetPath, gameFolder, collectionNode.FolderDecoration!), "*.Gbx"))
            {
                var decorationNode =
                    (CGameCtnDecoration?)await Gbx.ParseNodeAsync(decorationFilePath,
                        cancellationToken: cancellationToken);

                if (decorationNode?.DecoSize is null)
                {
                    throw new Exception("Not a decoration type or DecoSize is null");
                }
                
                var hash = HashStr($"GbxTools3D|Decoration|{gameFolder}|{collection.Name}|{decorationNode.DecoSize.Size.X}x{decorationNode.DecoSize.Size.Y}x{decorationNode.DecoSize.Size.Z}|Je te hais");

                var baseHeight = decorationNode.DecoSize.BaseHeightBase;
                // TODO
            }

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

                if (zoneDict.TryGetValue(blockName, out var zone))
                {
                    blockInfo.Height = (byte)zone.Height;
                }

                await ProcessBlockVariantsAsync(blockInfoNode.AirMobils, datasetPath, gameFolder, blockName,
                    isGround: false, blockInfo, usedMaterials, cancellationToken);
                await ProcessBlockVariantsAsync(blockInfoNode.GroundMobils, datasetPath, gameFolder, blockName,
                    isGround: true, blockInfo, usedMaterials, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            await outputCache.EvictByTagAsync("mesh", cancellationToken);
        }
    }

    private async Task ProcessBlockVariantsAsync(
        External<CSceneMobil>[][]? mobils,
        string datasetPath,
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
                    : Path.GetRelativePath(Path.Combine(datasetPath, gameFolder), mobil.File.GetFullPath());

                var solid = GetSolidFromMobil(variant, Path.Combine(datasetPath, gameFolder), out var path);

                if (solid is null)
                {
                    continue;
                }

                PopulateUsedMaterials(usedMaterials, solid, Path.Combine(datasetPath, gameFolder));

                var hash = HashStr(
                    $"GbxTools3D|Solid|{gameFolder}|{blockName}|{isGround}MyGuy|{i}|{j}|PleaseDontAbuseThisThankYou:*");

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
                    
                    var objectLinkSolid = GetSolidFromMobil(link.Mobil, Path.Combine(datasetPath, gameFolder), out var objectLinkSolidPath);
                    
                    if (objectLinkSolid is null)
                    {
                        continue;
                    }
                    
                    PopulateUsedMaterials(usedMaterials, solid, Path.Combine(datasetPath, gameFolder));

                    var solidHash = HashStr(
                        $"GbxTools3D|Solid|{gameFolder}|{blockName}|Hella{isGround}|{i}|{j}|{k}|marosisPakPakGhidraGang");
                    
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
                        : Path.GetRelativePath(Path.Combine(datasetPath, gameFolder), link.MobilFile.GetFullPath());
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

    private static void PopulateUsedMaterials(Dictionary<string, CPlugMaterial?> materials, CPlugSolid solid, string relativeTo)
    {
        var materialFiles = ((CPlugTree?)solid.Tree)?
            .GetAllChildren()
            .Where(x => x.ShaderFile is not null)
            .Select(x => (x.ShaderFile!, x.Shader as CPlugMaterial)) ?? [];

        foreach (var (materialFile, material) in materialFiles)
        {
            var materialRelPath = Path.GetRelativePath(relativeTo, materialFile.GetFullPath());
            materials.TryAdd(materialRelPath, material);
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

    private static CPlugSolid? GetSolidFromMobil(CSceneMobil mobil, string relativeTo, out string? relativePath)
    {
        if (mobil.Item?.Solid?.Tree is not CPlugSolid solid)
        {
            relativePath = null;
            return null;
        }

        relativePath = mobil.Item.Solid.TreeFile is null ? null
            : Path.GetRelativePath(relativeTo, mobil.Item.Solid.TreeFile.GetFullPath());
        return solid;
    }

    private static string HashStr(string str)
    {
        Span<byte> bytes = stackalloc byte[str.Length * 2];
        if (!Encoding.UTF8.TryGetBytes(str, bytes, out var bytesWritten))
        {
            throw new InvalidOperationException("Failed to encode string");
        }

        Span<byte> hash = stackalloc byte[32];
        
        if (!SHA256.TryHashData(bytes[..bytesWritten], hash, out _))
        {
            throw new InvalidOperationException("Failed to hash string");
        }
        
        return Convert.ToHexString(hash);
    }
}