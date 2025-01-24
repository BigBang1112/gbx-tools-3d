using System.Security.Cryptography;
using System.Text;
using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
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
        const string gameFolder = "TMF";

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
                    DisplayName = collectionNode.DisplayName ?? "",
                };

                await db.Collections.AddAsync(collection, cancellationToken);
            }

            collection.Name = collectionNode.DisplayName ?? "";

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

                var hash = HashStr($"GbxTools3D|Decoration|{gameFolder}|{decorationNode.Ident.Id}|Je te hais");

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

                await ProcessBlockVariantsAsync(blockInfoNode.AirMobils, datasetPath, gameFolder, blockName,
                    isGround: false, blockInfo, cancellationToken);
                await ProcessBlockVariantsAsync(blockInfoNode.GroundMobils, datasetPath, gameFolder, blockName,
                    isGround: true, blockInfo, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        if (mobils is not null)
        {
            for (var i = 0; i < mobils.Length; i++)
            {
                for (var j = 0; j < mobils[i].Length; j++)
                {
                    var variant = mobils[i][j].Node;

                    if (variant is null)
                    {
                        continue;
                    }

                    var solid = GetSolidFromMobil(variant, Path.Combine(datasetPath, gameFolder), out var path);
                    
                    if (solid is null)
                    {
                        continue;
                    }

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
                    blockVariant.Ground = true;
                    blockVariant.Variant = (byte)i;
                    blockVariant.SubVariant = (byte)j;
                    blockVariant.Mesh = mesh;
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