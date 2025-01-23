using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Enums;
using GbxTools3D.Serializers;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GbxTools3D.Services;

public sealed class PopulateDbService : BackgroundService
{
    private readonly IConfiguration config;
    private readonly IServiceProvider serviceProvider;
    private readonly IOutputCacheStore outputCache;
    private readonly ILogger<PopulateDbService> logger;

    public PopulateDbService(
        IConfiguration config, 
        IServiceProvider serviceProvider, 
        IOutputCacheStore outputCache, 
        ILogger<PopulateDbService> logger)
    {
        this.config = config;
        this.serviceProvider = serviceProvider;
        this.outputCache = outputCache;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await Task.Run(async () =>
    {
        var datasetPath = config["DatasetPath"];

        if (string.IsNullOrEmpty(datasetPath))
        {
            throw new InvalidOperationException("DatasetPath is not set in configuration");
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var gameFolder = "TMF";

        foreach (var vehicleFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, "Vehicles", "TrackManiaVehicle"), "*.Gbx"))
        {
            var modelNode = Gbx.ParseNode<CGameItemModel>(vehicleFilePath);

            if (modelNode.Vehicle is null)
            {
                continue;
            }

            if (modelNode.Vehicle.Item?.Solid?.Tree is not CPlugSolid solid)
            {
                continue;
            }

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"GbxTools3D|Vehicle|{gameFolder}|{modelNode.Ident.Id}|WhyDidYouNotHelpMe?")));

            var mesh = await GetOrCreateMeshAsync(db, hash, solid, (modelNode.Vehicle as CSceneVehicleCar)?.VehicleStruct, stoppingToken);

            // TODO reference with vehicle
        }

        foreach (var collectionFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, "Collections"), "*.Gbx"))
        {
            var collectionNode = Gbx.ParseNode<CGameCtnCollection>(collectionFilePath);

            var collection = await db.Collections.FirstOrDefaultAsync(x => x.Id == collectionNode.Collection, stoppingToken);

            if (collection is null)
            {
                collection = new Collection
                {
                    Id = collectionNode.Collection ?? "",
                    Name = collectionNode.DisplayName ?? "",
                };

                await db.Collections.AddAsync(collection, stoppingToken);
            }

            collection.Name = collectionNode.DisplayName ?? "";

            foreach (var decorationFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, collectionNode.FolderDecoration!), "*.Gbx"))
            {
                var decorationNode = (CGameCtnDecoration?)Gbx.ParseNode(decorationFilePath);

                if (decorationNode is null)
                {
                    continue;
                }

                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"GbxTools3D|Decoration|{gameFolder}|{decorationNode.Ident.Id}|Je te hais")));

                // TODO
            }

            foreach (var blockInfoFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, collectionNode.FolderBlockInfo!), "*.Gbx", SearchOption.AllDirectories))
            {
                var blockInfoNode = (CGameCtnBlockInfo?)Gbx.ParseNode(blockInfoFilePath);

                if (blockInfoNode is null)
                {
                    continue;
                }

                var blockName = blockInfoNode.Ident.Id;

                var blockInfo = await db.BlockInfos.FirstOrDefaultAsync(x => x.Name == blockName, stoppingToken);
                if (blockInfo is null)
                {
                    blockInfo = new BlockInfo
                    {
                        Collection = collection,
                        Name = blockName,
                    };
                    await db.BlockInfos.AddAsync(blockInfo, stoppingToken);
                }

                blockInfo.Name = blockName;
                blockInfo.AirUnits = blockInfoNode.AirBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];
                blockInfo.GroundUnits = blockInfoNode.GroundBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];

                await ProcessBlockVariantsAsync(db, blockInfoNode.AirMobils, gameFolder, blockName, isGround: false, blockInfo, stoppingToken);
                await ProcessBlockVariantsAsync(db, blockInfoNode.GroundMobils, gameFolder, blockName, isGround: true, blockInfo, stoppingToken);
            }

            await db.SaveChangesAsync(stoppingToken);
            await outputCache.EvictByTagAsync("mesh", stoppingToken);
        }
    }, stoppingToken);

    private async Task ProcessBlockVariantsAsync(
        AppDbContext db, 
        External<CSceneMobil>[][]? mobils, 
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

                    if (variant.Item?.Solid?.Tree is not CPlugSolid solid)
                    {
                        continue;
                    }

                    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"GbxTools3D|Solid|{gameFolder}|{blockName}|{isGround}MyGuy|{i}|{j}|PleaseDontAbuseThisThankYou:*")));

                    var mesh = await GetOrCreateMeshAsync(db, hash, solid, vehicle: null, cancellationToken);

                    var blockVariant = await db.BlockVariants
                        .FirstOrDefaultAsync(x => x.BlockInfoId == blockInfo.Id && x.Ground && x.Variant == i && x.SubVariant == j, cancellationToken);

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

    private async Task<Mesh> GetOrCreateMeshAsync(AppDbContext db, string hash, CPlugSolid solid, CPlugVehicleVisModelShared? vehicle, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing mesh {hash}", hash);

        var mesh = await db.Meshes.FirstOrDefaultAsync(x => x.Hash == hash, cancellationToken);

        var data = MeshSerializer.Serialize(solid, vehicle: vehicle);
        var dataLq = MeshSerializer.Serialize(solid, lod: 1, vehicle: vehicle);
        var dataELq = null as byte[];

        if (vehicle?.VisualVehicles.Length > 2)
        {
            dataELq = MeshSerializer.Serialize(solid, lod: 2, vehicle: vehicle);
        }

        if (mesh is null)
        {
            mesh = new Mesh
            {
                Hash = hash,
                Data = data,
            };
            await db.Meshes.AddAsync(mesh, cancellationToken);
        }

        mesh.Data = data;
        mesh.DataLQ = data.Length == dataLq.Length ? null : dataLq;
        mesh.DataELQ = dataELq is null || dataLq.Length == dataELq.Length ? null : dataELq;

        return mesh;
    }
}
