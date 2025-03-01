using GBX.NET;
using GbxTools3D.Client.Dtos;
using System.Net.Http.Json;

namespace GbxTools3D.Client.Services;

public interface IBlockClientService
{
    List<BlockInfoDto> Blocks { get; }

    Task<List<BlockInfoDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default);
    Task<BlockInfoDto?> GetAsync(GameVersion gameVersion, string collectionName, string blockName, CancellationToken cancellationToken = default);
    Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default);
}

internal sealed class BlockClientService : IBlockClientService
{
    private readonly HttpClient http;

    private (GameVersion, string) currentCollection;
    private readonly Dictionary<(GameVersion, string), List<BlockInfoDto>> cache = [];

    public List<BlockInfoDto> Blocks { get; private set; } = [];

    public BlockClientService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<List<BlockInfoDto>> GetAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue((gameVersion, collectionName), out var cachedBlocks))
        {
            return cachedBlocks;
        }

        using var response = await http.GetAsync($"/api/blocks/{gameVersion}/{collectionName}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var blocks = await response.Content.ReadFromJsonAsAsyncEnumerable(AppClientJsonContext.Default.BlockInfoDto, cancellationToken)
            .OfType<BlockInfoDto>()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        cache[(gameVersion, collectionName)] = blocks;

        return blocks;
    }

    public async Task<BlockInfoDto?> GetAsync(GameVersion gameVersion, string collectionName, string blockName, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"/api/blocks/{gameVersion}/{collectionName}/{blockName}", cancellationToken);

        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<BlockInfoDto>(cancellationToken) : null;
    }

    public async Task FetchAllAsync(GameVersion gameVersion, string collectionName, CancellationToken cancellationToken = default)
    {
        if (currentCollection == (gameVersion, collectionName))
        {
            return;
        }

        Blocks.Clear();
        Blocks.AddRange(await GetAllAsync(gameVersion, collectionName, cancellationToken));

        currentCollection = (gameVersion, collectionName);
    }
}