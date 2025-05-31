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
                .Include(x => x.Collection)
                .Include(x => x.Variants)
                    .ThenInclude(x => x.ObjectLinks)
                        .ThenInclude(x => x.Sound)
                .Where(x => x.Collection.GameVersion == gameVersion && x.Collection.Name == collectionName)
                .AsNoTracking()
                .ToListAsync(token);
            return blockInfos.Select(MapBlockInfo).ToList();
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task<BlockInfoDto?> GetAsync(GameVersion gameVersion, string collectionName, string blockName, CancellationToken cancellationToken)
    {
        var blockInfo = await db.BlockInfos
            .Include(x => x.Variants)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Collection.GameVersion == gameVersion && x.Collection.Name == collectionName && x.Name == blockName, cancellationToken);
        return blockInfo is null ? null : MapBlockInfo(blockInfo);
    }

    public async Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        Blocks = await GetAllAsync(gameVersion, collectionName, cancellationToken);
    }

    private static BlockInfoDto MapBlockInfo(BlockInfo blockInfo) => new()
    {
        Name = blockInfo.Name,
        AirUnits = blockInfo.AirUnits,
        GroundUnits = blockInfo.GroundUnits,
        HasAirHelper = blockInfo.HasAirHelper,
        HasGroundHelper = blockInfo.HasGroundHelper,
        HasConstructionModeHelper = blockInfo.HasConstructionModeHelper,
        AirVariants = blockInfo.Variants.Where(x => !x.Ground).Select(MapVariant).ToList(),
        GroundVariants = blockInfo.Variants.Where(x => x.Ground).Select(MapVariant).ToList(),
        Height = blockInfo.Height,
        IsDefaultZone = blockInfo.Collection.DefaultZoneBlock == blockInfo.Name,
        HasIcon = blockInfo.IconId.HasValue,
        IsRoad = blockInfo.IsRoad,
        PylonName = blockInfo.PylonName
    };

    private static BlockVariantDto MapVariant(BlockVariant variant) => new()
    {
        Variant = variant.Variant,
        SubVariant = variant.SubVariant,
        ObjectLinks = variant.ObjectLinks.Count == 0 ? null : variant.ObjectLinks.Select(x => new ObjectLinkDto
        {
            Location = x.Loc,
            SoundPath = x.Sound?.Path
        }).ToList()
    };
}
