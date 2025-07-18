using GBX.NET;
using GbxTools3D.Client.Dtos;
using System.Net.Http.Json;

namespace GbxTools3D.Client.Services;

public interface IVehicleClientService
{
    List<VehicleDto> Vehicles { get; }

    Task<List<VehicleDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken);
    Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken);
}

internal sealed class VehicleClientService : IVehicleClientService
{
    private readonly HttpClient http;

    private GameVersion currentGameVersion;
    private readonly Dictionary<GameVersion, List<VehicleDto>> cache = [];

    public List<VehicleDto> Vehicles { get; private set; } = [];

    public VehicleClientService(HttpClient http)
    {
        this.http = http;
    }

    public async Task<List<VehicleDto>> GetAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(gameVersion, out var cachedVehicles))
        {
            return cachedVehicles;
        }

        using var response = await http.GetAsync($"/api/vehicles/{gameVersion}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var vehicles = await response.Content.ReadFromJsonAsAsyncEnumerable(AppClientJsonContext.Default.VehicleDto, cancellationToken)
            .OfType<VehicleDto>()
            .ToListAsync(cancellationToken);

        cache[gameVersion] = vehicles;

        return vehicles;
    }

    public async Task FetchAllAsync(GameVersion gameVersion, CancellationToken cancellationToken = default)
    {
        if (currentGameVersion == gameVersion)
        {
            return;
        }

        Vehicles.Clear();
        Vehicles.AddRange(await GetAllAsync(gameVersion, cancellationToken));

        currentGameVersion = gameVersion;
    }
}