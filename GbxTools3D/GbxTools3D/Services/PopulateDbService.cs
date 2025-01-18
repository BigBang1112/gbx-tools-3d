using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Plug;
using GBX.NET.Engines.Scene;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Serializers;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GbxTools3D.Services;

public sealed class PopulateDbService : BackgroundService
{
    private readonly IConfiguration config;
    private readonly IServiceProvider serviceProvider;

    public PopulateDbService(IConfiguration config, IServiceProvider serviceProvider)
    {
        this.config = config;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) => await Task.Run(async () =>
    {
        var datasetPath = config["DatasetPath"];

        if (string.IsNullOrEmpty(datasetPath))
        {
            throw new InvalidOperationException("DatasetPath is not set in configuration");
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var gameFolder = "TMF";

        foreach (var collectionFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, "Collections"), "*.Gbx"))
        {
            var collectionNode = Gbx.ParseNode<CGameCtnCollection>(collectionFilePath);

            var collection = await context.Collections.FirstOrDefaultAsync(x => x.Id == collectionNode.Collection, stoppingToken);

            if (collection is null)
            {
                collection = new Collection
                {
                    Id = collectionNode.Collection ?? "",
                    Name = collectionNode.DisplayName ?? "",
                };

                await context.Collections.AddAsync(collection, stoppingToken);
            }

            collection.Name = collectionNode.DisplayName ?? "";

            foreach (var blockInfoFilePath in Directory.EnumerateFiles(Path.Combine(datasetPath, gameFolder, collectionNode.FolderBlockInfo!), "*.Gbx", SearchOption.AllDirectories))
            {
                var blockInfoNode = (CGameCtnBlockInfo?)Gbx.ParseNode(blockInfoFilePath);

                if (blockInfoNode is null)
                {
                    continue;
                }

                var blockName = blockInfoNode.Ident.Id;

                var blockInfo = await context.BlockInfos.FirstOrDefaultAsync(x => x.Name == blockName, stoppingToken);
                if (blockInfo is null)
                {
                    blockInfo = new BlockInfo
                    {
                        Collection = collection,
                        Name = blockName,
                    };
                    await context.BlockInfos.AddAsync(blockInfo, stoppingToken);
                }

                blockInfo.Name = blockName;
                blockInfo.AirUnits = blockInfoNode.AirBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];
                blockInfo.GroundUnits = blockInfoNode.GroundBlockUnitInfos?.Select(UnitInfoToBlockUnit).ToArray() ?? [];

                await ProcessBlockVariantsAsync(context, blockInfoNode.AirMobils, gameFolder, blockName, isGround: false, blockInfo, stoppingToken);
                await ProcessBlockVariantsAsync(context, blockInfoNode.GroundMobils, gameFolder, blockName, isGround: true, blockInfo, stoppingToken);
            }

            await context.SaveChangesAsync(stoppingToken);
        }
    }, stoppingToken);

    private static async Task ProcessBlockVariantsAsync(
        AppDbContext context, 
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

                    var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"GbxTools3D|Solid|{gameFolder}|{blockName}|{isGround.ToString().Reverse()}|{i}|{j}|PleaseDontAbuseThisThankYou:*"));
                    var hashStr = Convert.ToHexString(hash);

                    var mesh = await context.Meshes.FirstOrDefaultAsync(x => x.Hash == hashStr, cancellationToken);

                    var data = MeshSerializer.Serialize(solid);
                    var dataLq = MeshSerializer.Serialize(solid, lod: 1);

                    if (mesh is null)
                    {
                        mesh = new Mesh
                        {
                            Hash = hashStr,
                            Data = data,
                        };
                        await context.Meshes.AddAsync(mesh, cancellationToken);
                    }

                    mesh.Data = data;
                    mesh.DataLQ = data.Length == dataLq.Length ? null : dataLq;

                    var blockVariant = await context.BlockVariants
                        .FirstOrDefaultAsync(x => x.BlockInfoId == blockInfo.Id && x.Ground && x.Variant == i && x.SubVariant == j, cancellationToken);

                    if (blockVariant is null)
                    {
                        blockVariant = new BlockVariant
                        {
                            BlockInfo = blockInfo,
                            Mesh = mesh,
                        };

                        await context.BlockVariants.AddAsync(blockVariant, cancellationToken);
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
}
