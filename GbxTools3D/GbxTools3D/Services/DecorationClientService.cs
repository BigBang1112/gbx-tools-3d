using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using GbxTools3D.Data;
using GbxTools3D.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Services;

internal sealed class DecorationClientService : IDecorationClientService
{
    private readonly AppDbContext db;

    public List<DecorationSizeDto> DecorationSizes { get; private set; } = [];

    public DecorationClientService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<List<DecorationSizeDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken)
    {
        var decorations = await db.Decorations
            .Include(x => x.DecorationSize)
                .ThenInclude(x => x.Collection)
            .Where(x => x.DecorationSize.Collection.GameVersion == gameVersion && x.DecorationSize.Collection.Name == collectionName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return MapDecorations(decorations).ToList();
    }

    public async Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken)
    {
        DecorationSizes = await GetAllAsync(gameVersion, collectionName, cancellationToken);
    }

    private static IEnumerable<DecorationSizeDto> MapDecorations(IEnumerable<Decoration> decos) =>
        decos.GroupBy(x => new Int3(x.DecorationSize.SizeX, x.DecorationSize.SizeY, x.DecorationSize.SizeZ))
            .Select(decoGroup => new DecorationSizeDto
            {
                Size = decoGroup.Key,
                BaseHeight = decoGroup.First().DecorationSize.BaseHeight,
                Decorations = decoGroup.Select(x => new DecorationDto
                {
                    Name = x.Name,
                    Musics = x.Musics,
                    Sounds = x.Sounds,
                    Remap = x.Remap
                }).ToList(),
                Scene = decoGroup.First().DecorationSize.Scene
            });
}
