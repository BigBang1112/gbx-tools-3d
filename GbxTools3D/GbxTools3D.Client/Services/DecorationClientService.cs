using GBX.NET;
using GbxTools3D.Client.Dtos;
using System.Net.Http.Json;

namespace GbxTools3D.Client.Services;

public interface IDecorationClientService
{
    List<DecorationSizeDto> DecorationSizes { get; }

    Task<List<DecorationSizeDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken);
    Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken);
}

internal sealed class DecorationClientService : IDecorationClientService
{
    private readonly HttpClient http;

    private (GameVersion, string) currentCollection;
    private readonly Dictionary<(GameVersion, string), List<DecorationSizeDto>> cache = [];

    public List<DecorationSizeDto> DecorationSizes { get; private set; } = [];

    public DecorationClientService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<List<DecorationSizeDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue((gameVersion, collectionName), out var cachedBlocks))
        {
            return cachedBlocks;
        }

        using var response = await http.GetAsync($"/api/decorations/{gameVersion}/{collectionName}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var decorations = await response.Content.ReadFromJsonAsAsyncEnumerable(AppClientJsonContext.Default.DecorationSizeDto, cancellationToken)
            .OfType<DecorationSizeDto>()
            .ToListAsync(cancellationToken);

        cache[(gameVersion, collectionName)] = decorations;

        return decorations;
    }

    public async Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        if (currentCollection == (gameVersion, collectionName))
        {
            return;
        }

        DecorationSizes.Clear();
        DecorationSizes.AddRange(await GetAllAsync(gameVersion, collectionName, cancellationToken));

        currentCollection = (gameVersion, collectionName);
    }
}