using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GbxTools3D.Services;

public class CollectionClientService : ICollectionClientService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public List<CollectionDto> Collections { get; private set; } = [];

    public CollectionClientService(AppDbContext db, HybridCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<List<CollectionDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync($"collections:{gameVersion}", async (token) =>
        {
            return await db.Collections
                .Where(x => x.GameVersion == gameVersion)
                .Select(x => new CollectionDto
                {
                    Name = x.Name,
                    DisplayName = x.DisplayName,
                    UpdatedAt = x.UpdatedAt,
                    SquareHeight = x.SquareHeight,
                    SquareSize = x.SquareSize,
                    Vehicle = new IdentDto
                    {
                        Id = x.VehicleId,
                        Collection = x.VehicleCollection,
                        Author = x.VehicleAuthor
                    },
                    DefaultZoneBlock = x.DefaultZoneBlock,
                    SortIndex = x.SortIndex,
                    HasBlocks = x.BlockInfos.Count != 0,
                    HasDecorations = x.DecorationSizes.Count != 0,
                    HasVehicles = false,
                    HasItems = false,
                    HasMacroblocks = false
                })
                .OrderBy(x => x.SortIndex)
                .ToListAsync(token);
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        Collections = await GetAllAsync(gameVersion, cancellationToken);
    }
}
