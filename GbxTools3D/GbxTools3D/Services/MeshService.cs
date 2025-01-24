using GBX.NET.Engines.Plug;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using GbxTools3D.Serializers;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class MeshService
{
    private readonly AppDbContext db;
    private readonly ILogger<MeshService> logger;

    private static readonly Func<AppDbContext, string, Task<MeshHq?>> MeshHqFirstOrDefaultAsync = EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Meshes
        .Select(x => new MeshHq { Hash = x.Hash, Data = x.Data, CreatedAt = x.CreatedAt })
        .AsNoTracking()
        .FirstOrDefault(x => x.Hash == hash));
    
    private static readonly Func<AppDbContext, string, Task<Mesh?>> MeshFirstOrDefaultAsync =
        EF.CompileAsyncQuery((AppDbContext db, string hash) => db.Meshes.FirstOrDefault(x => x.Hash == hash));

    public MeshService(AppDbContext db, ILogger<MeshService> logger)
    {
        this.db = db;
        this.logger = logger;
    }
    
    public async Task<MeshHq?> GetMeshByHashHqAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await MeshHqFirstOrDefaultAsync(db, hash);
    }

    public async Task<int> GetMeshCountAsync(CancellationToken cancellationToken = default)
    {
        return await db.Meshes.CountAsync(cancellationToken);
    }

    public async Task<Mesh> GetOrCreateMeshAsync(string hash, string? path, CPlugSolid solid, CPlugVehicleVisModelShared? vehicle, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        logger.LogInformation("New mesh: {Hash} (path: {Path})", hash, path);

        var mesh = await MeshFirstOrDefaultAsync(db, hash);

        var data = MeshSerializer.Serialize(solid, vehicle: vehicle);
        var dataLq = MeshSerializer.Serialize(solid, lod: 1, vehicle: vehicle);
        var dataELq = default(byte[]);

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
                Path = path,
            };
            await db.Meshes.AddAsync(mesh, cancellationToken);
        }

        mesh.Data = data;
        mesh.DataLQ = data.Length == dataLq.Length ? null : dataLq;
        mesh.DataELQ = dataELq is null || dataLq.Length == dataELq.Length ? null : dataELq;

        return mesh;
    }

    internal class MeshHq
    {
        public required string Hash { get; init; }
        public required byte[] Data { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}