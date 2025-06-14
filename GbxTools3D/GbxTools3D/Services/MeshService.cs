using GBX.NET.Engines.Plug;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Models;
using GbxTools3D.Serializers;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class MeshService
{
    private readonly AppDbContext db;
    private readonly ILogger<MeshService> logger;

    private static readonly Func<AppDbContext, string, Task<CacheableHiddenData?>> MeshHqFirstOrDefaultAsync = EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Meshes
        .Select(x => new CacheableHiddenData { Hash = x.Hash, Data = x.Data, UpdatedAt = x.UpdatedAt })
        .AsNoTracking()
        .FirstOrDefault(x => x.Hash == hash));

    private static readonly Func<AppDbContext, string, Task<CacheableHiddenData?>> MeshSurfFirstOrDefaultAsync = EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Meshes
        .Where(x => x.DataSurf != null)
        .Select(x => new CacheableHiddenData { Hash = x.Hash, Data = x.DataSurf!, UpdatedAt = x.UpdatedAt })
        .AsNoTracking()
        .FirstOrDefault(x => x.Hash == hash));
    
    private static readonly Func<AppDbContext, string, Task<Mesh?>> MeshFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Meshes.FirstOrDefault(x => x.Hash == hash));

    public MeshService(AppDbContext db, ILogger<MeshService> logger)
    {
        this.db = db;
        this.logger = logger;
    }
    
    public async Task<CacheableHiddenData?> GetMeshHqByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await MeshHqFirstOrDefaultAsync(db, hash);
    }
    
    public async Task<CacheableHiddenData?> GetMeshSurfByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await MeshSurfFirstOrDefaultAsync(db, hash);
    }

    public async Task<int> GetMeshCountAsync(CancellationToken cancellationToken = default)
    {
        return await db.Meshes.CountAsync(cancellationToken);
    }

    public async Task<Mesh> GetOrCreateMeshAsync(
        string gamePath, 
        string hash, 
        string? path, 
        CPlugSolid solid, 
        CPlugVehicleVisModelShared? vehicle, 
        bool isDeco = false, 
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mesh = await MeshFirstOrDefaultAsync(db, hash);

        var data = MeshSerializer.Serialize(solid, gamePath, vehicle: vehicle, isDeco: isDeco);
        var dataLq = MeshSerializer.Serialize(solid, gamePath, lod: 1, vehicle: vehicle, isDeco: isDeco);
        var dataELq = default(byte[]);

        if (vehicle?.VisualVehicles.Length > 2)
        {
            dataELq = MeshSerializer.Serialize(solid, gamePath, lod: 2, vehicle: vehicle, isDeco: isDeco);
        }

        if (mesh is null)
        {
            logger.LogInformation("New mesh: {Hash} (path: {Path})", hash, path);
            
            mesh = new Mesh
            {
                Hash = hash,
                Data = data,
                Path = path,
            };
            await db.Meshes.AddAsync(mesh, cancellationToken);
        }

        mesh.Data = data;
        mesh.DataLQ = data.Length == dataLq.Length ? null : dataLq;
        mesh.DataVLQ = dataELq is null || dataLq.Length == dataELq.Length ? null : dataELq;
        mesh.DataSurf = MeshSerializer.Serialize(solid, gamePath, vehicle: vehicle, collision: true);
        mesh.UpdatedAt = DateTime.UtcNow;

        return mesh;
    }

    public async Task<Mesh> GetOrCreateMeshAsync(
        string gamePath,
        string hash,
        string? path,
        CPlugSolid2Model solid,
        CPlugVehicleVisModelShared? vehicle,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var mesh = await MeshFirstOrDefaultAsync(db, hash);

        var data = MeshSerializer.Serialize(solid, gamePath, vehicle: vehicle);
        var dataLq = MeshSerializer.Serialize(solid, gamePath, lod: 1, vehicle: vehicle);
        var dataELq = default(byte[]);

        if (vehicle?.VisualVehicles.Length > 2)
        {
            dataELq = MeshSerializer.Serialize(solid, gamePath, lod: 2, vehicle: vehicle);
        }

        if (mesh is null)
        {
            logger.LogInformation("New mesh: {Hash} (path: {Path})", hash, path);

            mesh = new Mesh
            {
                Hash = hash,
                Data = data,
                Path = path,
            };
            await db.Meshes.AddAsync(mesh, cancellationToken);
        }

        mesh.Data = data;
        mesh.DataLQ = data.Length == dataLq.Length ? null : dataLq;
        mesh.DataVLQ = dataELq is null || dataLq.Length == dataELq.Length ? null : dataELq;
        mesh.UpdatedAt = DateTime.UtcNow;

        return mesh;
    }
}