using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GbxTools3D.Services;

internal sealed class VehicleClientService : IVehicleClientService
{
    private readonly AppDbContext db;
    private readonly HybridCache cache;

    public List<VehicleDto> Vehicles { get; private set; } = [];

    public VehicleClientService(AppDbContext db, HybridCache cache)
    {
        this.db = db;
        this.cache = cache;
    }

    public async Task<List<VehicleDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"vehicles:{gameVersion}", async (token) =>
        {
            return await db.Vehicles
                .Where(x => x.GameVersion == gameVersion)
                .Select(x => new VehicleDto
                {
                    Name = x.Name,
                    CameraFov = x.CameraFov,
                    CameraFar = x.CameraFar,
                    CameraUp = x.CameraUp,
                    CameraLookAtFactor = x.CameraLookAtFactor,
                    HasIcon = x.IconId.HasValue
                })
                .AsNoTracking()
                .ToListAsync(token);
        }, new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(1) }, cancellationToken: cancellationToken);
    }

    public async Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken)
    {
        Vehicles = await GetAllAsync(gameVersion, cancellationToken);
    }
}
