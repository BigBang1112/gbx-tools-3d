using GBX.NET;
using GbxTools3D.Client.Dtos;
using GbxTools3D.Client.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GbxTools3D.Endpoints.Api;

public static class VehicleApiEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{gameVersion}", GetVehicles)
            .CacheOutput(x => x.Tag("vehicle"));
    }

    private static async Task<Ok<List<VehicleDto>>> GetVehicles(
        IVehicleClientService vehicleClientService,
        GameVersion gameVersion,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var vehicles = await vehicleClientService.GetAllAsync(gameVersion, cancellationToken);

        context.Response.Headers.CacheControl = "max-age=3600";

        return TypedResults.Ok(vehicles);
    }
}