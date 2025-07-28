using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GbxTools3D.Services;

public sealed class BlockClientService : IBlockClientService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public List<BlockInfoDto> Blocks { get; private set; } = [];

    public BlockClientService(AppDbContext db, HybridCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<List<BlockInfoDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"blocks:{gameVersion}:{collectionName}", async (token) =>
        {
            var blockInfos = await db.BlockInfos
                .Include(x => x.TerrainModifier)
                .Include(x => x.Collection)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.ObjectLinks)
                        .ThenInclude(x => x.Sound)
                .Where(x => x.Collection.GameVersion == gameVersion && x.Collection.Name == collectionName)
                .Select(x => MapBlockInfo(x, false))
                .ToListAsync(token);
            return blockInfos;
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task<BlockInfoDto?> GetAsync(GameVersion gameVersion, string collectionName, string blockName, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"block:{gameVersion}:{collectionName}:{blockName}", async (token) =>
        {
            var blockInfo = await db.BlockInfos
                .Include(x => x.TerrainModifier)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.Mesh)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.ObjectLinks)
                        .ThenInclude(x => x.Sound)
                .Include(x => x.Collection)
                .Where(x => x.Collection.GameVersion == gameVersion && x.Collection.Name == collectionName && x.Name == blockName)
                .Select(x => MapBlockInfo(x, true))
                .FirstOrDefaultAsync(cancellationToken);
            return blockInfo;
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        Blocks = await GetAllAsync(gameVersion, collectionName, cancellationToken);
    }

    private static BlockInfoDto MapBlockInfo(BlockInfo blockInfo, bool detailed) => new()
    {
        Name = blockInfo.Name,
        AirUnits = blockInfo.AirUnits,
        GroundUnits = blockInfo.GroundUnits,
        HasAirHelper = blockInfo.HasAirHelper,
        HasGroundHelper = blockInfo.HasGroundHelper,
        HasConstructionModeHelper = blockInfo.HasConstructionModeHelper,
        HasAirWaypoint = blockInfo.HasAirWaypoint,
        HasGroundWaypoint = blockInfo.HasGroundWaypoint,
        AirVariants = blockInfo.Variants.Where(x => !x.Ground).Select(x => MapVariant(x, detailed)).ToList(),
        GroundVariants = blockInfo.Variants.Where(x => x.Ground).Select(x => MapVariant(x, detailed)).ToList(),
        Height = blockInfo.Height,
        IsDefaultZone = blockInfo.Collection.DefaultZoneBlock == blockInfo.Name,
        HasIcon = blockInfo.IconId.HasValue,
        IsRoad = blockInfo.IsRoad,
        PylonName = blockInfo.PylonName,
        TerrainModifier = blockInfo.TerrainModifier?.Name,
    };

    private static BlockVariantDto MapVariant(BlockVariant variant, bool detailed) => new()
    {
        Variant = variant.Variant,
        SubVariant = variant.SubVariant,
        MobilPath = detailed ? variant.Path : null,
        MeshPath = detailed ? variant.Mesh?.Path : null,
        ObjectLinks = variant.ObjectLinks.Count == 0 ? null : variant.ObjectLinks.Select(x => new ObjectLinkDto
        {
            Location = x.Loc,
            SoundPath = x.Sound?.Path
        }).ToList(),
    };
}
