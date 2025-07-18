using GBX.NET;
using GbxTools3D.Client.Dtos;
using System.Net.Http.Json;

namespace GbxTools3D.Client.Services;

public interface ICollectionClientService
{
    List<CollectionDto> Collections { get; }
    Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default);
    Task<List<CollectionDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default);
}

internal sealed class CollectionClientService : ICollectionClientService
{
    private readonly HttpClient http;

    private GameVersion currentGameVersion;
    public List<CollectionDto> Collections { get; private set; } = [];

    public CollectionClientService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<List<CollectionDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"/api/collections/{gameVersion}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return (await response.Content.ReadFromJsonAsync(AppClientJsonContext.Default.ListCollectionDto, cancellationToken) ?? [])
            .OrderBy(x => x.SortIndex)
            .ToList();
    }

    public async Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        if (currentGameVersion == gameVersion)
        {
            return;
        }

        Collections = await GetAllAsync(gameVersion, cancellationToken);

        currentGameVersion = gameVersion;
    }
}
